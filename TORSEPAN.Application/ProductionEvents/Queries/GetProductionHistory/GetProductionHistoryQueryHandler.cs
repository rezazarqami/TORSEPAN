using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionHistory;

public sealed class GetProductionHistoryQueryHandler
    : IRequestHandler<GetProductionHistoryQuery, IReadOnlyCollection<GetProductionHistoryResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProductionHistoryQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<GetProductionHistoryResponse>> Handle(
        GetProductionHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var events = await _unitOfWork.ProductionEvents
            .GetByHandpanIdAsync(request.HandpanId);

        return events
            .OrderByDescending(x => x.EventDate)
            .Select(x => new GetProductionHistoryResponse
            {
                EventId = x.Id,
                Action = x.Action.ToString(),
                Result = x.Result.ToString(),
                EventDate = x.EventDate,
                Description = x.Description
            })
            .ToList();
    }
}