Exit code: 0
Wall time: 0.6 seconds
Output:
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
    public List<string> Notes { get; set; } = [];
}

