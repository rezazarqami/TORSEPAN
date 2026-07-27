namespace TORSEPAN.Panel.Models;

public sealed record HandpanDto(
    Guid Id,
    string SerialNumber,
    string Stage,
    DateTime CreatedAt);