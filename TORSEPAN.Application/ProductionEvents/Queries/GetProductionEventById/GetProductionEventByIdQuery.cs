using MediatR;
using TORSEPAN.Application.ProductionEvents.DTOs;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionEventById;

public sealed record GetProductionEventByIdQuery(Guid Id)
    : IRequest<ProductionEventDto>;