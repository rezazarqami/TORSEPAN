namespace TORSEPAN.Application.Handpans.Queries.GetAllHandpans;

public sealed record HandpanDto(
    Guid Id,
    string SerialNumber,
    string TopBowlCode,
    string BottomBowlCode,
    string MaterialName,
    string ScaleName,
    int Status,
    int Stage,
    DateTime CreatedAt,
    IReadOnlyList<HandpanOperationDto> Operations);

public sealed record HandpanOperationDto(
    int Action,
    string PerformedBy,
    DateTime PerformedAt);
