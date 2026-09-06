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
        var selectedTehran = request.SelectedDate?.Date ?? tehranNow.Date;
        if (calendar.GetYear(selectedTehran) != year || calendar.GetMonth(selectedTehran) != month)
            selectedTehran = tehranNow.Date;
        var selectedDay = calendar.GetDayOfMonth(selectedTehran);
        var monthStartTehran = calendar.ToDateTime(year, month, 1, 0, 0, 0, 0);
        var monthStartUtc = DateTime.SpecifyKind(monthStartTehran.AddHours(-3.5), DateTimeKind.Utc);
        var allEvents = await _unitOfWork.ProductionEvents.GetReportAsync(null, null, null, null, EventResult.Completed);
        var bowlStageDates = allEvents.Where(x => x.BowlId.HasValue && !x.Description.StartsWith("NOTE:"))
            .GroupBy(x => x.BowlId!.Value)
            .ToDictionary(x => x.Key, x => x.Max(e => e.EventDate));
        var events = allEvents.Where(x => x.EventDate >= monthStartUtc).ToList();
        var tracked = new[] { ProductionAction.Dimple, ProductionAction.Shape, ProductionAction.Design, ProductionAction.Furnace, ProductionAction.Glue, ProductionAction.Tune, ProductionAction.FineTune, ProductionAction.QualityCheck, ProductionAction.Packaging };
        List<MonthlyUserOperationResponse> Summarize(IEnumerable<TORSEPAN.Domain.Entities.ProductionEvent> source) => source.Where(x => tracked.Contains(x.Action) &&
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
                    : x.Key.Action == ProductionAction.Design
                        ? x.Count()
                        : x.Count(),
                DisplayOrder=x.Key.DisplayOrder
            })
            .Where(x => x.Count > 0)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.UserName).ThenBy(x => x.Operation).ToList();
        var monthly = Summarize(events);
        var selectedStartUtc = DateTime.SpecifyKind(selectedTehran.AddHours(-3.5), DateTimeKind.Utc);
        var daily = Summarize(allEvents.Where(x => x.EventDate >= selectedStartUtc && x.EventDate < selectedStartUtc.AddDays(1)));
        var firstDay = calendar.ToDateTime(year, month, 1, 0, 0, 0, 0);

        return new GetProductionDashboardResponse
        {
            TotalHandpans = allHandpans.Count,
            Finished = finished,
            Rejected = rejected,
            InProduction = allHandpans.Count - finished - rejected,
            CompletionRate = allHandpans.Count == 0 ? 0 : Math.Round((double)finished / allHandpans.Count * 100, 2),
            CurrentPersianMonthTitle = $"{PersianMonthName(month)} {year}",
            MonthlyUserOperations = monthly,
            DailyUserOperations = daily,
            PersianYear = year,
            PersianMonth = month,
            SelectedPersianDay = selectedDay,
            DaysInMonth = calendar.GetDaysInMonth(year, month),
            FirstDayOffset = ((int)firstDay.DayOfWeek + 1) % 7,
            Queues =
            [
                BowlQueue("آماده دیمپل", ProductionStage.WaitingForDimple),
                GroupedBowlQueue("آماده شیپ", ProductionStage.WaitingForShape, ProductionAction.Dimple, splitByBowlType: true),
                BowlQueue("آماده پخت", ProductionStage.WaitingForBake),
                GroupedBowlQueue("آماده تیون", ProductionStage.WaitingForTune, ProductionAction.Shape, splitByBowlType: true),
                GroupedBowlQueue("آماده چسب — کاسه رو", ProductionStage.WaitingForGlue, ProductionAction.Tune, BowlType.Top),
                GroupedBowlQueue("آماده چسب — کاسه زیر", ProductionStage.WaitingForGlue, ProductionAction.Tune, BowlType.Bottom),
                HandpanQueue("اتاق چسب", ProductionStage.GlueRoom),
                SplitBowlQueue("آماده بسته‌بندی صادراتی", ProductionStage.WaitingForExportPackaging),
                GroupedHandpanQueue("آماده فاین تیون", ProductionStage.WaitingForFinalTune),
                HandpanQueue("آماده کنترل کیفیت (QC)", ProductionStage.WaitingForQualityControl),
                HandpanQueue("آماده بسته‌بندی", ProductionStage.WaitingForPackaging),
                HandpanQueue("انبار سازها", ProductionStage.FinishedWarehouse)
            ]
        };

        ProductionQueueItemResponse BowlQueue(string title, ProductionStage stage) => new()
        {
            Stage = title,
            Codes = bowls.Where(x => x.Stage == stage).Select(x => x.ProductionCode).OrderBy(x => x).ToList(),
            Items = bowls.Where(x => x.Stage == stage).Select(x => BowlItem(x.Id, x.ProductionCode)).OrderBy(x => x.Code).ToList()
        };

        ProductionQueueItemResponse HandpanQueue(string title, ProductionStage stage, bool colorByAge = true) => new()
        {
            Stage = title,
            ColorByAge = colorByAge,
            Codes = allHandpans.Where(x => x.Stage == stage).Select(x => x.SerialNumber).OrderBy(x => x).ToList(),
            Items = allHandpans.Where(x => x.Stage == stage).Select(HandpanItem).OrderBy(x => x.Code).ToList()
        };

        ProductionQueueItemResponse GroupedBowlQueue(string title, ProductionStage stage,
            ProductionAction action, BowlType? type = null, bool splitByBowlType = false)
        {
            var items = bowls.Where(x => x.Stage == stage && (!type.HasValue || x.BowlType == type.Value)).ToList();
            return new ProductionQueueItemResponse
            {
                Stage = title,
                Codes = items.Select(x => x.ProductionCode).OrderBy(x => x).ToList(),
                Items = items.Select(x => BowlItem(x.Id, x.ProductionCode)).OrderBy(x => x.Code).ToList(),
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
                        Codes = x.Select(b => b.ProductionCode).OrderBy(code => code).ToList(),
                        Items = x.Select(b => BowlItem(b.Id, b.ProductionCode)).OrderBy(item => item.Code).ToList()
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
                Items = items.Select(HandpanItem).OrderBy(x => x.Code).ToList(),
                Groups = items.GroupBy(x => PerformerForBowl(x.Assembly.TopBowlId, ProductionAction.Tune))
                    .OrderBy(x => x.Key)
                    .Select(x => new ProductionQueueGroupResponse
                    {
                        UserName = string.IsNullOrWhiteSpace(x.Key) ? "نامشخص" : x.Key,
                        Codes = x.Select(h => h.SerialNumber).OrderBy(code => code).ToList(),
                        Items = x.Select(HandpanItem).OrderBy(item => item.Code).ToList()
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

        ProductionQueueCodeResponse BowlItem(Guid bowlId, string code) => new()
        {
            Code = code,
            DaysInStage = DaysSince(bowlStageDates.GetValueOrDefault(bowlId))
        };

        ProductionQueueItemResponse SplitBowlQueue(string title, ProductionStage stage)
        {
            var items = bowls.Where(x => x.Stage == stage).ToList();
            return new ProductionQueueItemResponse
            {
                Stage = title,
                Codes = items.Select(x => x.ProductionCode).OrderBy(x => x).ToList(),
                Items = items.Select(x => BowlItem(x.Id, x.ProductionCode)).OrderBy(x => x.Code).ToList(),
                Groups = items.GroupBy(x => x.BowlType).OrderBy(x => x.Key)
                    .Select(x => new ProductionQueueGroupResponse
                    {
                        BowlTypeLabel = x.Key == BowlType.Top ? "کاسه رو" : "کاسه زیر",
                        Codes = x.Select(b => b.ProductionCode).OrderBy(code => code).ToList(),
                        Items = x.Select(b => BowlItem(b.Id, b.ProductionCode)).OrderBy(item => item.Code).ToList()
                    }).ToList()
            };
        }

        ProductionQueueCodeResponse HandpanItem(TORSEPAN.Domain.Entities.Handpan handpan) => new()
        {
            Code = handpan.SerialNumber,
            DaysInStage = DaysSince(handpan.UpdatedAt ?? handpan.CreatedAt)
        };

        int DaysSince(DateTime? enteredAt) => enteredAt.HasValue
            ? Math.Max(0, (int)(DateTime.UtcNow - enteredAt.Value).TotalDays)
            : 0;
    }

    private static string OperationTitle(ProductionAction action) => action switch
    { ProductionAction.Dimple=>"دیمپل",ProductionAction.Shape=>"شیپ",ProductionAction.Design=>"دیزاین",ProductionAction.Furnace=>"پخت",ProductionAction.Glue=>"چسب",ProductionAction.Tune=>"تیون",ProductionAction.FineTune=>"فاین تیون",ProductionAction.QualityCheck=>"کنترل کیفیت",ProductionAction.Packaging=>"بسته‌بندی",_=>action.ToString() };
    private static string BowlTypeSuffix(BowlType? bowlType) => bowlType switch
    { BowlType.Top => " کاسه رو", BowlType.Bottom => " کاسه زیر", _ => string.Empty };
    private static string PersianMonthName(int month) => new[] { "", "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" }[month];
}
