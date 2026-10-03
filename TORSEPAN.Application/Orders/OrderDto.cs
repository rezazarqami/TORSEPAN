namespace TORSEPAN.Application.Orders;

public sealed record CreateOrderRequest(string CustomerName, Guid ScaleId, int DurationDays);
public sealed record AssignOrderCodeRequest(string Code);
public sealed record OrderDto(Guid Id, string CustomerName, Guid ScaleId, string ScaleName,
    int DurationDays, DateTime CreatedAtUtc, DateTime DueAtUtc, string? InstrumentCode,
    string ProductionStage, string ProductionStatus, IReadOnlyList<string> CompletedOperations,
    IReadOnlyList<OrderReminderDto> Reminders);
public sealed record OrderReminderDto(int Milestone, DateTime DueAtUtc, DateTime? SentAtUtc, bool HasError);

public static class OrderTiming
{
    public static double ElapsedDays(DateTime start, DateTime now) => Math.Max(0, (now - start).TotalDays);
    public static double RemainingDays(DateTime due, DateTime now) => Math.Max(0, (due - now).TotalDays);
    public static double Progress(DateTime start, DateTime due, DateTime now) =>
        Math.Clamp((now - start).TotalSeconds / (due - start).TotalSeconds * 100, 0, 100);
}
