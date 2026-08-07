namespace TORSEPAN.Application.Bowls.Queries.GetAllBowls;

public sealed class BowlDto
{
    public Guid Id { get; init; }

    public string ProductionCode { get; init; } = string.Empty;

    public int BowlType { get; init; }

    public bool HasNotes { get; init; }

    public int InstrumentType { get; init; }

    public Guid MaterialId { get; init; }

    public string MaterialName { get; init; } = string.Empty;

    public int Status { get; init; }

    public int Stage { get; init; }

    public IReadOnlyList<BowlOperationDto> Operations { get; init; } = [];
}

public sealed class BowlOperationDto
{
    public int Action { get; init; }
    public string PerformedBy { get; init; } = string.Empty;
    public DateTime PerformedAt { get; init; }
}
