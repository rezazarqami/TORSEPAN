using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionReport;

public sealed class GetProductionReportQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetProductionReportQuery, GetProductionReportResponse>
{
    public async Task<GetProductionReportResponse> Handle(GetProductionReportQuery request, CancellationToken cancellationToken)
    {
        var events = await unitOfWork.ProductionEvents.GetReportAsync(request.From?.Date,
            request.To?.Date.AddDays(1), request.UserId, request.Action, request.Result);
        var users = (await unitOfWork.Users.GetAllAsync()).OrderBy(x => x.FullName).ToList();
        var activities = events.Select(x => new ProductionActivityItem
        {
            Id = x.Id, EventDate = x.EventDate, UserId = x.UserId, UserName = x.User.UserName,
            FullName = x.User.FullName, Action = (int)x.Action, ActionTitle = ActionTitle(x.Action),
            Result = (int)x.Result, ResultTitle = ResultTitle(x.Result), DurationMinutes = DurationMinutes(x),
            DurationTitle = DurationTitle(x), ProductionCode = x.Bowl?.ProductionCode ?? x.Handpan?.SerialNumber ?? "",
            Description = x.Description.StartsWith("PACKAGING_ITEMS:")
                ? $"اقلام بسته‌بندی: {x.Description[16..].Replace('|', '،')}" : x.Description
        }).ToList();

        var performance = activities.GroupBy(x => new { x.UserId, x.UserName, x.FullName }).Select(x => new UserPerformanceItem
        {
            UserId = x.Key.UserId, UserName = x.Key.UserName, FullName = x.Key.FullName,
            OperationCount = x.Count(), CompletedCount = x.Count(y => y.Result == (int)EventResult.Completed),
            DurationMinutes = x.Sum(y => y.DurationMinutes ?? 0)
        }).OrderByDescending(x => x.OperationCount).ToList();

        return new GetProductionReportResponse
        {
            TotalOperations = activities.Count,
            CompletedOperations = activities.Count(x => x.Result == (int)EventResult.Completed),
            RejectedOrFailedOperations = activities.Count(x => x.Result is (int)EventResult.Failed or (int)EventResult.Rejected),
            TotalDurationMinutes = activities.Sum(x => x.DurationMinutes ?? 0),
            Users = users.Select(x => new ReportUserItem { Id = x.Id, UserName = x.UserName, FullName = x.FullName }).ToList(),
            UserPerformance = performance, Activities = activities
        };
    }

    private static int? DurationMinutes(ProductionEvent item) => item.Duration.HasValue
        ? item.Duration == OperationDuration.Over60 ? 65 : (int)item.Duration.Value * 5 : null;
    private static string DurationTitle(ProductionEvent item) => item.Duration.HasValue
        ? item.Duration == OperationDuration.Over60 ? "بیشتر از ۶۰ دقیقه" : $"{(int)item.Duration.Value * 5} دقیقه" : "ثبت نشده";
    private static string ActionTitle(ProductionAction action) => action switch
    {
        ProductionAction.Created => "ثبت اولیه", ProductionAction.Dimple => "دیمپل", ProductionAction.Shape => "شیپ",
        ProductionAction.Furnace => "پخت", ProductionAction.Glue => "چسب", ProductionAction.Tune => "تیون",
        ProductionAction.FineTune => "فاین‌تیون", ProductionAction.QualityCheck => "کنترل کیفیت",
        ProductionAction.Packaging => "بسته‌بندی", ProductionAction.WarehouseEntry => "ورود به انبار",
        ProductionAction.Reject => "برگشتی", ProductionAction.Sale => "فروش", _ => action.ToString()
    };
    private static string ResultTitle(EventResult result) => result switch
    {
        EventResult.Completed => "تکمیل‌شده", EventResult.Failed => "ناموفق",
        EventResult.Rejected => "ردشده", EventResult.Skipped => "ردشده از مرحله", _ => result.ToString()
    };
}

