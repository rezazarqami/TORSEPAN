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
    DateTime CreatedAt);
