using MediatR;
using TORSEPAN.Application.Common.Results;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed record CompleteHandpanQualityControlCommand(
    string ProductionCode,
    bool Approved,
    string? RejectionReason,
    string? Details,
    int QualityWarehouseId = 1,
    TORSEPAN.Domain.Enums.ExportWarehouseLocation? ExportWarehouseLocation = null) : IRequest<Result<BowlDimpleDto>>;
