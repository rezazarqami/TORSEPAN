namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed class BowlDimpleDto
{
    public Guid Id { get; init; }
    public string ProductionCode { get; init; } = string.Empty;
    public int BowlType { get; init; }
    public bool HasNotes { get; init; }
    public int InstrumentType { get; init; }
    public int Status { get; init; }
    public int Stage { get; init; }
    public string ScaleName { get; set; } = "نامشخص";
    public bool IsHandpanScale { get; set; }
    public List<string> Notes { get; init; } = [];
    public List<BowlStageHistoryDto> History { get; init; } = [];
}

public sealed class BowlStageHistoryDto
{
    public int Action { get; init; }
    public string ActionTitle { get; init; } = string.Empty;
    public string PerformedBy { get; init; } = string.Empty;
    public DateTime PerformedAt { get; init; }
}
