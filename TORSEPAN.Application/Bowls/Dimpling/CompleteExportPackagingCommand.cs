using MediatR;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed record CompleteExportPackagingCommand(string ProductionCode, ExportWarehouseLocation ExportWarehouseLocation) : IRequest<Result<BowlDimpleDto>>;
