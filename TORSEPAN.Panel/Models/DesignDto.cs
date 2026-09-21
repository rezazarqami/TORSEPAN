namespace TORSEPAN.Panel.Models;
public sealed class DesignTypeDto { public Guid Id { get; set; } public string Name { get; set; } = string.Empty; public decimal Rate { get; set; } public decimal ExportRate { get; set; } }
public sealed class DesignUserDto { public Guid Id { get; set; } public string Name { get; set; } = string.Empty; }
public sealed class BowlDesignDto { public Guid EventId { get; set; } public Guid DesignTypeId { get; set; } public string Name { get; set; } = string.Empty; public string PerformedBy { get; set; } = string.Empty; public DateTime RegisteredAt { get; set; } }
public sealed class BowlDesignLookupDto { public Guid BowlId { get; set; } public string ProductionCode { get; set; } = string.Empty; public string BowlType { get; set; } = string.Empty; public string MaterialName { get; set; } = string.Empty; public List<BowlDesignDto> Designs { get; set; } = []; }
