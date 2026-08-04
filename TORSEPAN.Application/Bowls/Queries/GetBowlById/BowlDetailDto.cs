namespace TORSEPAN.Application.Bowls.Queries.GetBowlById;

public sealed class BowlDetailDto
{
    public Guid Id { get; init; }

    public string ProductionCode { get; init; } = string.Empty;

    public int BowlType { get; init; }

    public bool HasNotes { get; init; }

    public int InstrumentType { get; init; }public int Status { get; init; }

    public int Stage { get; init; }
}