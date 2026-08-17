namespace TORSEPAN.Panel.Models;

public sealed class BowlDto
{
    public Guid Id { get; set; }

    public string ProductionCode { get; set; } = string.Empty;

    public int BowlType { get; set; }

    public bool HasNotes { get; set; }

    public int InstrumentType { get; set; }

    public Guid MaterialId { get; set; }

    public string MaterialName { get; set; } = string.Empty;
    public Guid? ScaleId { get; set; }
    public string ScaleName { get; set; } = string.Empty;

    public int Status { get; set; }

    public int Stage { get; set; }

    public List<ProductionOperationDto> Operations { get; set; } = [];
}
