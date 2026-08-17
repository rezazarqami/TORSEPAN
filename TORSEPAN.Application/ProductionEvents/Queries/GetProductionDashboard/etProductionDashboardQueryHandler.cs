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
        var allHandpans = (await _unitOfWork.Handpans.GetAllAsync()).ToList();
        var handpans = (await _unitOfWork.Handpans.GetAllWithAssemblyAsync()).ToList();
        var finished = allHandpans.Count(x => x.Stage == ProductionStage.FinishedWarehouse);
        var rejected = allHandpans.Count(x => x.Stage == ProductionStage.Rejected);
        var tehranNow = DateTime.UtcNow.AddHours(3.5);
        var calendar = new PersianCalendar();
        var year = calendar.GetYear(tehranNow); var month = calendar.GetMonth(tehranNow);
        var monthStartTehran = calendar.ToDateTime(year, month, 1, 0, 0, 0, 0);
        var monthStartUtc = DateTime.SpecifyKind(monthStartTehran.AddHours(-3.5), DateTimeKind.Utc);
        var allEvents = await _unitOfWork.ProductionEvents.GetReportAsync(null, null, null, null, EventResult.Completed);
        var events = allEvents.Where(x => x.EventDate >= monthStartUtc).ToList();
        var tracked = new[] { ProductionAction.Dimple, ProductionAction.Shape, ProductionAction.Furnace, ProductionAction.Glue, ProductionAction.Tune, ProductionAction.FineTune };
        var monthly = events.Where(x => tracked.Contains(x.Action) &&
                                        !x.Description.StartsWith("NOTE:") &&
                                        x.Description != "Released from glue room")
            .GroupBy(x => new
            {
                x.UserId, x.User.UserName, x.User.FullName, x.User.DisplayOrder, x.Action,
                BowlType = x.Action is ProductionAction.Dimple or ProductionAction.Shape or ProductionAction.Tune
                    ? x.Bowl == null ? (BowlType?)null : x.Bowl.BowlType
                    : null
            })
            .Select(x => new MonthlyUserOperationResponse
            {
                UserName = string.IsNullOrWhiteSpace(x.Key.FullName) ? x.Key.UserName : x.Key.FullName,
                Operation = OperationTitle(x.Key.Action) + BowlTypeSuffix(x.Key.BowlType),
                Count = x.Key.Action == ProductionAction.Glue
                    ? x.Where(e => e.HandpanId.HasValue &&
                                   e.Description.StartsWith("Glued with bowl"))
                        .Select(e => e.HandpanId).Distinct().Count()
                    : x.Count(),
                DisplayOrder=x.Key.DisplayOrder
            })
            .Where(x => x.Count > 0)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.UserName).ThenBy(x => x.Operation).ToList();

        return new GetProductionDashboardResponse
        {
            TotalHandpans = allHandpans.Count,
            Finished = finished,
            Rejected = rejected,
            InProduction = allHandpans.Count - finished - rejected,
            CompletionRate = allHandpans.Count == 0 ? 0 : Math.Round((double)finished / allHandpans.Count * 100, 2),
            CurrentPersianMonthTitle = $"{PersianMonthName(month)} {year}",
            MonthlyUserOperations = monthly,
            Queues =
            [
                BowlQueue("آماده دیمپل", ProductionStage.WaitingForDimple),
                GroupedBowlQueue("آماده شیپ", ProductionStage.WaitingForShape, ProductionAction.Dimple, splitByBowlType: true),
                GroupedBowlQueue("آماده تیون", ProductionStage.WaitingForTune, ProductionAction.Shape, splitByBowlType: true),
                GroupedBowlQueue("آماده چسب — کاسه رو", ProductionStage.WaitingForGlue, ProductionAction.Tune, BowlType.Top),
                GroupedBowlQueue("آماده چسب — کاسه زیر", ProductionStage.WaitingForGlue, ProductionAction.Tune, BowlType.Bottom),
                BowlQueue("آماده بسته‌بندی صادراتی", ProductionStage.WaitingForExportPackaging),
                GroupedHandpanQueue("آماده فاین تیون", ProductionStage.WaitingForFinalTune),
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

        ProductionQueueItemResponse GroupedBowlQueue(string title, ProductionStage stage,
            ProductionAction action, BowlType? type = null, bool splitByBowlType = false)
        {
            var items = bowls.Where(x => x.Stage == stage && (!type.HasValue || x.BowlType == type.Value)).ToList();
            return new ProductionQueueItemResponse
            {
                Stage = title,
                Codes = items.Select(x => x.ProductionCode).OrderBy(x => x).ToList(),
                Groups = items.GroupBy(x => new
                    {
                        UserName = PerformerForBowl(x.Id, action),
                        BowlType = splitByBowlType ? x.BowlType : (BowlType?)null
                    })
                    .OrderBy(x => x.Key.UserName).ThenBy(x => x.Key.BowlType)
                    .Select(x => new ProductionQueueGroupResponse
                    {
                        UserName = x.Key.UserName,
                        BowlTypeLabel = x.Key.BowlType.HasValue
                            ? x.Key.BowlType == BowlType.Top ? "کاسه رو" : "کاسه زیر"
                            : string.Empty,
                        Codes = x.Select(b => b.ProductionCode).OrderBy(code => code).ToList()
                    }).ToList()
            };
        }

        ProductionQueueItemResponse GroupedHandpanQueue(string title, ProductionStage stage)
        {
            var items = handpans.Where(x => x.Stage == stage).ToList();
            return new ProductionQueueItemResponse
            {
                Stage = title,
                Codes = items.Select(x => x.SerialNumber).OrderBy(x => x).ToList(),
                Groups = items.GroupBy(x =>
                    string.Join(" / ", new[] { x.Assembly.TopBowlId, x.Assembly.BottomBowlId }
                        .Select(id => PerformerForBowl(id, ProductionAction.Tune))
                        .Distinct().OrderBy(name => name)))
                    .OrderBy(x => x.Key)
                    .Select(x => new ProductionQueueGroupResponse
                    {
                        UserName = string.IsNullOrWhiteSpace(x.Key) ? "نامشخص" : x.Key,
                        Codes = x.Select(h => h.SerialNumber).OrderBy(code => code).ToList()
                    }).ToList()
            };
        }

        string PerformerForBowl(Guid bowlId, ProductionAction action)
        {
            var productionEvent = allEvents.Where(x => x.BowlId == bowlId && x.Action == action &&
                                                        !x.Description.StartsWith("NOTE:"))
                .OrderByDescending(x => x.EventDate).FirstOrDefault();
            if (productionEvent is null) return "نامشخص";
            return string.IsNullOrWhiteSpace(productionEvent.User.FullName)
                ? productionEvent.User.UserName
                : productionEvent.User.FullName;
        }
    }

    private static string OperationTitle(ProductionAction action) => action switch
    { ProductionAction.Dimple=>"دیمپل",ProductionAction.Shape=>"شیپ",ProductionAction.Furnace=>"پخت",ProductionAction.Glue=>"چسب",ProductionAction.Tune=>"تیون",ProductionAction.FineTune=>"فاین تیون",_=>action.ToString() };
    private static string BowlTypeSuffix(BowlType? bowlType) => bowlType switch
    { BowlType.Top => " کاسه رو", BowlType.Bottom => " کاسه زیر", _ => string.Empty };
    private static string PersianMonthName(int month) => new[] { "", "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" }[month];
}
