namespace TORSEPAN.API.Contracts.Bowls;

public sealed record CompleteHandpanQualityControlRequest(
    bool Approved,
    string? RejectionReason,
    string? Details);
