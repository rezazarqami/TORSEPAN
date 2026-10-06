namespace TORSEPAN.Application.Orders;

public sealed record CreateOrderRequest(string CustomerName, Guid ScaleId, int DurationDays, DateOnly? OrderDate = null);
public sealed record AssignOrderCodeRequest(string Code, string? ExpectedCode = null);
public sealed record OrderDto(Guid Id, string CustomerName, Guid ScaleId, string ScaleName,
    int DurationDays, DateTime CreatedAtUtc, DateTime DueAtUtc, string? InstrumentCode,
    string ProductionStage, string ProductionStatus, IReadOnlyList<string> CompletedOperations,
    IReadOnlyList<OrderReminderDto> Reminders)
{
    public bool IsDraft { get; init; }
    public int Version { get; init; } = 1;
    public IReadOnlyList<OrderLineDto> Lines { get; init; } = [];
    public int TotalQuantity => Lines.Count == 0 ? 1 : Lines.Sum(x => x.Quantity);
    public int AssignedQuantity => Lines.Count == 0 ? (InstrumentCode is null ? 0 : 1) : Lines.Sum(x => x.Instruments.Count);
}
public sealed record OrderLineRequest(Guid ScaleId, Guid? DesignTypeId, int Quantity);
public sealed record SaveOrderDraftRequest(string CustomerName, int DurationDays, DateOnly? OrderDate,
    IReadOnlyList<OrderLineRequest> Lines, int Version = 1);
public sealed record FinalizeOrderRequest(int Version);
public sealed record OrderLineDto(Guid Id, int Position, Guid ScaleId, string ScaleName,
    Guid? DesignTypeId, string DesignName, int Quantity, IReadOnlyList<OrderInstrumentDto> Instruments);
public sealed record OrderInstrumentDto(int Slot, string Code, string ProductionStage,
    string ProductionStatus, IReadOnlyList<string> CompletedOperations);
public sealed record OrderReminderDto(int Milestone, DateTime DueAtUtc, DateTime? SentAtUtc, bool HasError);

public static class OrderTiming
{
    public static double ElapsedDays(DateTime start, DateTime now) => Math.Max(0, (now - start).TotalDays);
    public static double RemainingDays(DateTime due, DateTime now) => Math.Max(0, (due - now).TotalDays);
    public static double Progress(DateTime start, DateTime due, DateTime now) =>
        Math.Clamp((now - start).TotalSeconds / (due - start).TotalSeconds * 100, 0, 100);
}
