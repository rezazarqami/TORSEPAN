namespace TORSEPAN.Application.Handpans.Queries.GetHandpanById;

public sealed record HandpanDetailDto(
    Guid Id,
    string SerialNumber,
    string ProductionCode,
    string Stage,
    DateTime CreatedAt);