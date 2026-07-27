namespace TORSEPAN.Application.Handpans.Queries.GetHandpansByStatus;

public sealed record HandpanDto(
    Guid Id,
    string SerialNumber,
    string Stage,
    string Status,
    DateTime CreatedAt);