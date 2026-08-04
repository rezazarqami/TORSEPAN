using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Features.Bowls.DTOs;

public class BowlDto
{
    public Guid Id { get; set; }
    public string ProductionCode { get; set; } = string.Empty;
    public BowlType BowlType { get; set; }
    public bool HasNotes { get; set; }
    public InstrumentType InstrumentType { get; set; }
    public ProductionStatus Status { get; set; }
    public ProductionStage Stage { get; set; }
}
