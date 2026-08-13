using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Enums;
using System.Globalization;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionDashboard;

public sealed class GetProductionDashboardQueryHandler
    : IRequestHandler<GetProductionDashboardQuery, GetProductionDashboardResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    public GetProductionDashboardQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<GetProductionDashboardResponse> Handle(
        GetProductionDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var bowls = (await _unitOfWork.Bowls.GetAllAsync()).ToList();
        var handpans = (await _unitOfWork.Handpans.GetAllAsync()).ToList();
        var finished = handpans.Count(x => x.Stage == ProductionStage.FinishedWarehouse);
        var rejected = handpans.Count(x => x.Stage == ProductionStage.Rejected);
        var tehranNow = DateTime.UtcNow.AddHours(3.5);
        var calendar = new PersianCalendar();
        var year = calendar.GetYear(tehranNow); var month = calendar.GetMonth(tehranNow);
        var monthStartTehran = calendar.ToDateTime(year, month, 1, 0, 0, 0, 0);
        var monthStartUtc = DateTime.SpecifyKind(monthStartTehran.AddHours(-3.5), DateTimeKind.Utc);
        var events = await _unitOfWork.ProductionEvents.GetReportAsync(monthStartUtc, null, null, null, EventResult.Completed);
        var tracked = new[] { ProductionAction.Dimple, ProductionAction.Shape, ProductionAction.Furnace, ProductionAction.Glue, ProductionAction.Tune, ProductionAction.FineTune };
        var monthly = events.Where(x => tracked.Contains(x.Action) && !x.Description.StartsWith("NOTE:"))
            .GroupBy(x => new { x.UserId, x.User.UserName, x.User.FullName, x.User.DisplayOrder, x.Action })
            .Select(x => new MonthlyUserOperationResponse
            {
                UserName = string.IsNullOrWhiteSpace(x.Key.FullName) ? x.Key.UserName : x.Key.FullName,
                Operation = OperationTitle(x.Key.Action),
                Count = x.Key.Action == ProductionAction.Glue
                    ? x.Where(e => e.HandpanId.HasValue).Select(e => e.HandpanId).Distinct().Count()
                    : x.Count(),
                DisplayOrder=x.Key.DisplayOrder
            }).OrderBy(x => x.DisplayOrder).ThenBy(x => x.UserName).ThenBy(x => x.Operation).ToList();

        return new GetProductionDashboardResponse
        {
            TotalHandpans = handpans.Count,
            Finished = finished,
            Rejected = rejected,
            InProduction = handpans.Count - finished - rejected,
            CompletionRate = handpans.Count == 0 ? 0 : Math.Round((double)finished / handpans.Count * 100, 2),
            CurrentPersianMonthTitle = $"{PersianMonthName(month)} {year}",
            MonthlyUserOperations = monthly,
            Queues =
            [
                BowlQueue("آماده دیمپل", ProductionStage.WaitingForDimple),
                BowlQueue("آماده شیپ", ProductionStage.WaitingForShape),
                BowlQueue("آماده تیون", ProductionStage.WaitingForTune),
                BowlQueueByType("آماده چسب — کاسه رو", ProductionStage.WaitingForGlue, BowlType.Top),
                BowlQueueByType("آماده چسب — کاسه زیر", ProductionStage.WaitingForGlue, BowlType.Bottom),
                BowlQueue("آماده بسته‌بندی صادراتی", ProductionStage.WaitingForExportPackaging),
                HandpanQueue("آماده فاین تیون", ProductionStage.WaitingForFinalTune),
                HandpanQueue("آماده کنترل کیفیت (QC)", ProductionStage.WaitingForQualityControl),
                HandpanQueue("آماده بسته‌بندی", ProductionStage.WaitingForPackaging)
            ]
        };

        ProductionQueueItemResponse BowlQueue(string title, ProductionStage stage) => new()
        {
            Stage = title,
            Codes = bowls.Where(x => x.Stage == stage).Select(x => x.ProductionCode).OrderBy(x => x).ToList()
        };

        ProductionQueueItemResponse HandpanQueue(string title, ProductionStage stage) => new()
        {
            Stage = title,
            Codes = handpans.Where(x => x.Stage == stage).Select(x => x.SerialNumber).OrderBy(x => x).ToList()
        };

        ProductionQueueItemResponse BowlQueueByType(string title, ProductionStage stage, BowlType type) => new()
        {
            Stage = title,
            Codes = bowls.Where(x => x.Stage == stage && x.BowlType == type).Select(x => x.ProductionCode).OrderBy(x => x).ToList()
        };
    }

    private static string OperationTitle(ProductionAction action) => action switch
    { ProductionAction.Dimple=>"دیمپل",ProductionAction.Shape=>"شیپ",ProductionAction.Furnace=>"پخت",ProductionAction.Glue=>"چسب",ProductionAction.Tune=>"تیون",ProductionAction.FineTune=>"فاین تیون",_=>action.ToString() };
    private static string PersianMonthName(int month) => new[] { "", "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" }[month];
}
