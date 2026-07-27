namespace TORSEPAN.API.Common;

public sealed record ApiError(
    string Code,
    string Message);