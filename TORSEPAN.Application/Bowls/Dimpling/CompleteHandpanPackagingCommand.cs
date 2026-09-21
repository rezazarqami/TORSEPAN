using MediatR;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed record CompleteHandpanPackagingCommand(string ProductionCode, IReadOnlyCollection<Guid> MaterialIds, ExportWarehouseLocation? ExportWarehouseLocation)
    : IRequest<Result<BowlDimpleDto>>;
