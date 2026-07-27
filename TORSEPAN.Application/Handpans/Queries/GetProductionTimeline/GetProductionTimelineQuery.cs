using MediatR;
using TORSEPAN.Application.Handpans.DTOs;

namespace TORSEPAN.Application.Handpans.Queries.GetProductionTimeline;

public sealed record GetProductionTimelineQuery(
    string SerialNumber)
    : IRequest<ProductionTimelineDto>;