using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Application.ProductionEvents.DTOs;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionEventsByHandpan;

public sealed class GetProductionEventsByHandpanQueryHandler
    : IRequestHandler<GetProductionEventsByHandpanQuery, GetProductionEventsByHandpanQueryResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProductionEventsByHandpanQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetProductionEventsByHandpanQueryResponse> Handle(
        GetProductionEventsByHandpanQuery request,
        CancellationToken cancellationToken)
    {
        var events = await _unitOfWork.ProductionEvents
            .GetByHandpanIdAsync(request.HandpanId);

        return new GetProductionEventsByHandpanQueryResponse
        {
            HandpanId = request.HandpanId,
            TotalEvents = events.Count,
            Events = events
                .Select(x => new ProductionEventDto
                {
                    Id = x.Id,
                    Action = x.Action.ToString(),
                    Result = x.Result.ToString(),
                    EventDate = x.EventDate,
                    Description = x.Description
                })
                .ToList()
        };
    }
}