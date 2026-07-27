using MediatR;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetHandpanDetails;

public sealed record GetHandpanDetailsQuery(Guid HandpanId)
    : IRequest<GetHandpanDetailsResponse>;