namespace TORSEPAN.Application.ProductionEvents.Commands.CreateProductionEvent;

public sealed class CreateProductionEventCommandResponse
{
    public Guid Id { get; init; }

    public Guid HandpanId { get; init; }

    public string Action { get; init; } = string.Empty;

    public string Result { get; init; } = string.Empty;

    public DateTime EventDate { get; init; }

    public string Message { get; init; } = string.Empty;
}