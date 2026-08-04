namespace TORSEPAN.Panel.Models;

public sealed class CreateBowlRequest
{
    public string ProductionCode { get; set; } = string.Empty;

    public int BowlType { get; set; }

    public bool HasNotes { get; set; }

    public int InstrumentType { get; set; }

    public Guid MaterialId { get; set; }
}