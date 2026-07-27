using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionQueue;

public sealed class GetProductionQueueQueryHandler
    : IRequestHandler<GetProductionQueueQuery, IReadOnlyCollection<GetProductionQueueResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProductionQueueQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<GetProductionQueueResponse>> Handle(
        GetProductionQueueQuery request,
        CancellationToken cancellationToken)
    {
        var handpans = await _unitOfWork.Handpans.GetAllAsync();

        return handpans
            .Where(x => x.Stage.ToString().Equals(request.Stage, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.CreatedAt)
            .Select(x => new GetProductionQueueResponse
            {
                HandpanId = x.Id,
                SerialNumber = x.SerialNumber,
                Stage = x.Stage.ToString(),
                CreatedAt = x.CreatedAt
            })
            .ToList();
    }
}