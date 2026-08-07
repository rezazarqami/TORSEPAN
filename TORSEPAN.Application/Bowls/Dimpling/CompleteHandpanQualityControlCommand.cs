using MediatR;
using TORSEPAN.Application.Common.Results;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed record CompleteHandpanQualityControlCommand(
    string ProductionCode,
    bool Approved,
    string? RejectionReason,
    string? Details) : IRequest<Result<BowlDimpleDto>>;
