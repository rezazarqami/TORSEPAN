using MediatR;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetWarehouseInventory;

public sealed record GetWarehouseInventoryQuery(Guid? HandpanId = null)
    : IRequest<IReadOnlyCollection<GetWarehouseInventoryResponse>>;
