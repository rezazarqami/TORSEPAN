using MediatR;

namespace TORSEPAN.Application.Bowls.Queries.GetExportWarehouse;

public sealed record GetExportWarehouseQuery : IRequest<IReadOnlyList<ExportWarehouseItemDto>>;

public sealed record ExportWarehouseItemDto(Guid Id, string ProductionCode, int BowlType, string MaterialName);
