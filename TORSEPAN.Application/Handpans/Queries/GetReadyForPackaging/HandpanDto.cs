namespace TORSEPAN.Application.Handpans.Queries.GetReadyForPackaging;

public sealed record HandpanDto(
    Guid Id,
    string SerialNumber,
    string Stage,
    DateTime CreatedAt);