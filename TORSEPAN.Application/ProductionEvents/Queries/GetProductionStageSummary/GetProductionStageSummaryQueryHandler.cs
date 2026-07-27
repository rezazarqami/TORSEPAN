using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionStageSummary;

public sealed class GetProductionStageSummaryQueryHandler
    : IRequestHandler<GetProductionStageSummaryQuery, IReadOnlyCollection<GetProductionStageSummaryResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProductionStageSummaryQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<GetProductionStageSummaryResponse>> Handle(
        GetProductionStageSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var handpans = await _unitOfWork.Handpans.GetAllAsync();

        return handpans
            .GroupBy(x => x.Stage.ToString())
            .Select(x => new GetProductionStageSummaryResponse
            {
                Stage = x.Key,
                Count = x.Count()
            })
            .OrderBy(x => x.Stage)
            .ToList();
    }
}