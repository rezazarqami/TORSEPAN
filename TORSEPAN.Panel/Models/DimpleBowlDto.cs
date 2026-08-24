namespace TORSEPAN.Panel.Models;

public sealed class DimpleBowlDto
{
    public Guid Id { get; set; }
    public string ProductionCode { get; set; } = string.Empty;
    public int BowlType { get; set; }
    public bool HasNotes { get; set; }
    public int InstrumentType { get; set; }
    public int Status { get; set; }
    public int Stage { get; set; }
    public string ScaleName { get; set; } = "نامشخص";
    public bool IsHandpanScale { get; set; }
    public string HandpanCode { get; set; } = string.Empty;
    public string TopBowlCode { get; set; } = string.Empty;
    public string BottomBowlCode { get; set; } = string.Empty;
    public List<string> Notes { get; set; } = [];
    public List<BowlStageHistoryDto> History { get; set; } = [];
}

public sealed class BowlStageHistoryDto
{
    public int Action { get; set; }
    public string ActionTitle { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
}
