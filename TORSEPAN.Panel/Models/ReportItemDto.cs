namespace TORSEPAN.Panel.Models;

public sealed record ReportItemDto(
    string ProductionCode,
    string Stage,
    DateTime Date,
    string User);