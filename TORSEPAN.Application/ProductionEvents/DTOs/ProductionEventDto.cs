namespace TORSEPAN.Application.ProductionEvents.DTOs;

public sealed class ProductionEventDto
{
    public Guid Id { get; init; }

    public Guid? HandpanId { get; init; }

    public Guid? AssemblyId { get; init; }

    public Guid? BowlId { get; init; }

    public Guid UserId { get; init; }

    public string Action { get; init; } = string.Empty;

    public string Result { get; init; } = string.Empty;

    public string? Description { get; init; }

    public int? Duration { get; init; }

    public DateTime EventDate { get; init; }
}