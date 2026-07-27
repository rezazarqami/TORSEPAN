using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetStageWorkload;

public sealed class GetStageWorkloadQueryHandler
    : IRequestHandler<GetStageWorkloadQuery, IReadOnlyCollection<GetStageWorkloadResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetStageWorkloadQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<GetStageWorkloadResponse>> Handle(
        GetStageWorkloadQuery request,
        CancellationToken cancellationToken)
    {
        var handpans = await _unitOfWork.Handpans.GetAllAsync();

        return handpans
            .GroupBy(x => x.Stage.ToString())
            .Select(x => new GetStageWorkloadResponse
            {
                Stage = x.Key,
                Count = x.Count()
            })
            .OrderBy(x => x.Stage)
            .ToList();
    }
}