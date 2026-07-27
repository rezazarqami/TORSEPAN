using MediatR;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Handpans.Queries.GetHandpansByStatus;

public sealed record GetHandpansByStatusQuery(
    ProductionStatus Status)
    : IRequest<IReadOnlyList<HandpanDto>>;