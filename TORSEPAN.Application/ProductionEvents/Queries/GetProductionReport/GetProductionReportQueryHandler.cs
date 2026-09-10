using MediatR;
using TORSEPAN.Application.Common.Reporting;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionReport;

public sealed class GetProductionReportQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetProductionReportQuery, GetProductionReportResponse>
{
    public async Task<GetProductionReportResponse> Handle(GetProductionReportQuery request, CancellationToken cancellationToken)
    {
        var events = await unitOfWork.ProductionEvents.GetReportAsync(ToUtcBoundary(request.From),
            ToUtcBoundary(request.To, 1), request.UserId, request.Action, request.Result);
        var users = (await unitOfWork.Users.GetAllAsync()).OrderBy(x => x.FullName).ToList();
        var activities = events.Where(x => x.Description != "Released from glue room" && !x.Description.StartsWith("MATERIAL_STOCK:")).Select(x => new ProductionActivityItem
        {
            Id = x.Id, EventDate = x.EventDate, UserId = x.UserId, UserName = x.User.UserName,
            FullName = x.User.FullName, Action = (int)x.Action, ActionTitle = ActionTitle(x),
            Result = (int)x.Result, ResultTitle = ResultTitle(x.Result), DurationMinutes = DurationMinutes(x),
            DurationTitle = DurationTitle(x), ProductionCode = x.Bowl?.ProductionCode ?? x.Handpan?.SerialNumber ?? "",
            Description = x.Description.StartsWith("PACKAGING_ITEMS:")
                ? $"اقلام بسته‌بندی: {x.Description[16..].Replace('|', '،')}" : x.Description
        }).ToList();

        var performance = activities.GroupBy(x => new { x.UserId, x.UserName, x.FullName }).Select(x => new UserPerformanceItem
        {
            UserId = x.Key.UserId, UserName = x.Key.UserName, FullName = x.Key.FullName,
            OperationCount = x.Count(), CompletedCount = x.Count(y => y.Result == (int)EventResult.Completed),
            DurationMinutes = x.Sum(y => y.DurationMinutes ?? 0),
            TimedOperations = x.Where(y => y.DurationMinutes.HasValue)
                .GroupBy(y => new { y.Action, y.ActionTitle })
                .Select(y => new TimedOperationPerformanceItem
                {
                    Action = y.Key.Action,
                    ActionTitle = y.Key.ActionTitle,
                    Count = y.Count(),
                    TotalDurationMinutes = y.Sum(z => z.DurationMinutes!.Value),
                    AverageDurationMinutes = Math.Round(y.Average(z => z.DurationMinutes!.Value), 1)
                }).OrderBy(y => y.Action).ToList(),
            UntimedOperations = x.Where(y => !y.DurationMinutes.HasValue)
                .GroupBy(y => new { y.Action, y.ActionTitle })
                .Select(y => new UntimedOperationPerformanceItem
                {
                    Action = y.Key.Action,
                    ActionTitle = y.Key.ActionTitle,
                    Count = y.Count()
                }).OrderBy(y => y.Action).ToList()
        }).OrderByDescending(x => x.TimedOperations.Sum(y => y.Count))
            .ThenByDescending(x => x.OperationCount).ToList();

        var trends = request.UserId.HasValue
            ? await BuildTrendsAsync(request, cancellationToken)
            : (Operations: (IReadOnlyList<ReportTrendItem>)[], Duration: (IReadOnlyList<ReportTrendItem>)[]);
        var userTrend = request.UserId.HasValue ? new UserTrendSummaryItem
        {
            CurrentOperationCount = trends.Operations.LastOrDefault()?.Count ?? 0,
            PreviousOperationCount = trends.Operations.Count > 1 ? trends.Operations[^2].Count : 0,
            AverageOperationCount = trends.Operations.LastOrDefault()?.Average ?? 0,
            CurrentDurationMinutes = trends.Duration.LastOrDefault()?.Count ?? 0,
            PreviousDurationMinutes = trends.Duration.Count > 1 ? trends.Duration[^2].Count : 0,
            AverageDurationMinutes = trends.Duration.LastOrDefault()?.Average ?? 0
        } : null;
        return new GetProductionReportResponse
        {
            TotalOperations = activities.Count,
            CompletedOperations = activities.Count(x => x.Result == (int)EventResult.Completed),
            RejectedOrFailedOperations = activities.Count(x => x.Result is (int)EventResult.Failed or (int)EventResult.Rejected),
            TotalDurationMinutes = activities.Sum(x => x.DurationMinutes ?? 0),
            Users = users.Select(x => new ReportUserItem { Id = x.Id, UserName = x.UserName, FullName = x.FullName }).ToList(),
            UserPerformance = performance, Activities = activities, Trend = trends.Operations,
            DurationTrend = trends.Duration, UserTrend = userTrend
        };
    }

    private async Task<(IReadOnlyList<ReportTrendItem> Operations, IReadOnlyList<ReportTrendItem> Duration)> BuildTrendsAsync(GetProductionReportQuery request, CancellationToken ct)
    {
        var local=(request.To??DateTime.UtcNow.AddHours(3.5)).Date;
        var months=PersianMonthCalendar.LastMonths(local,6);
        var events=await unitOfWork.ProductionEvents.GetReportAsync(months[0].UtcStart,months[^1].UtcEnd,request.UserId,request.Action,null);
        var counts=months.Select(month=>new ReportTrendItem(month.Label,events.Count(x=>x.EventDate>=month.UtcStart&&x.EventDate<month.UtcEnd&&!x.Description.StartsWith("MATERIAL_STOCK:")),0)).ToList();
        var durations=months.Select(month=>new ReportTrendItem(month.Label,events.Where(x=>x.EventDate>=month.UtcStart&&x.EventDate<month.UtcEnd&&!x.Description.StartsWith("MATERIAL_STOCK:")).Sum(x=>DurationMinutes(x)??0),0)).ToList();
        var countAverage=counts.Count==0?0:Math.Round(counts.Average(x=>x.Count),1);
        var durationAverage=durations.Count==0?0:Math.Round(durations.Average(x=>x.Count),1);
        return (counts.Select(x=>x with{Average=countAverage}).ToList(),durations.Select(x=>x with{Average=durationAverage}).ToList());
    }

    private static DateTime? ToUtcBoundary(DateTime? date, int addDays = 0) => date.HasValue
        ? DateTime.SpecifyKind(date.Value.Date.AddDays(addDays).AddHours(-3.5), DateTimeKind.Utc)
        : null;

    private static int? DurationMinutes(ProductionEvent item) => item.Duration.HasValue
        ? item.Duration == OperationDuration.Over60 ? 65 : (int)item.Duration.Value * 5 : null;
    private static string DurationTitle(ProductionEvent item) => item.Duration.HasValue
        ? item.Duration == OperationDuration.Over60 ? "بیشتر از ۶۰ دقیقه" : $"{(int)item.Duration.Value * 5} دقیقه" : "ثبت نشده";
    private static string ActionTitle(ProductionEvent item) => item.Action switch
    {
        ProductionAction.Created => "ثبت اولیه", ProductionAction.Dimple => "دیمپل", ProductionAction.Shape => "شیپ",
        ProductionAction.Furnace => "پخت", ProductionAction.Glue => "چسب", ProductionAction.Tune => "تیون",
        ProductionAction.FineTune => "فاین‌تیون", ProductionAction.Design => "دیزاین", ProductionAction.QualityCheck => "کنترل کیفیت",
        ProductionAction.Packaging => item.BowlId.HasValue ? "بسته‌بندی صادراتی" : "بسته‌بندی عادی", ProductionAction.WarehouseEntry => "ورود به انبار",
        ProductionAction.Reject => "برگشتی", ProductionAction.Sale => "فروش", _ => item.Action.ToString()
    };
    private static string ResultTitle(EventResult result) => result switch
    {
        EventResult.Completed => "تکمیل‌شده", EventResult.Failed => "ناموفق",
        EventResult.Rejected => "ردشده", EventResult.Skipped => "ردشده از مرحله", _ => result.ToString()
    };
}
