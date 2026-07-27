using MediatR;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetWarehouseInventory;

public sealed record GetWarehouseInventoryQuery()
    : IRequest<IReadOnlyCollection<GetWarehouseInventoryResponse>>;