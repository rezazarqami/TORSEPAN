namespace TORSEPAN.Application.Handpans.Queries.GetAllHandpans;

public sealed record HandpanDto(
    Guid Id,
    string SerialNumber,
    string ProductionCode,
    string Stage,
    DateTime CreatedAt);