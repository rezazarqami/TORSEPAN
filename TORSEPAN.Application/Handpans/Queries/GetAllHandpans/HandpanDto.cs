namespace TORSEPAN.Application.Handpans.Queries.GetAllHandpans;

public sealed record HandpanDto(
    Guid Id,
    string SerialNumber,
    string TopBowlCode,
    string BottomBowlCode,
    int Status,
    int Stage,
    DateTime CreatedAt);
