using MediatR;

namespace TORSEPAN.Application.ProductionEvents.Commands.MoveToWarehouse;

public sealed record MoveToWarehouseCommand(Guid HandpanId)
    : IRequest;