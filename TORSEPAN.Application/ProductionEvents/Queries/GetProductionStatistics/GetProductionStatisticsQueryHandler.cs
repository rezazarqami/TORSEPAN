using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionStatistics;

public sealed class GetProductionStatisticsQueryHandler
    : IRequestHandler<GetProductionStatisticsQuery, GetProductionStatisticsResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProductionStatisticsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetProductionStatisticsResponse> Handle(
        GetProductionStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        var handpans = await _unitOfWork.Handpans.GetAllAsync();

        return new GetProductionStatisticsResponse
        {
            TotalHandpans = handpans.Count(),

            TotalCompletedHandpans =
                handpans.Count(x => x.Stage == ProductionStage.FinishedWarehouse),

            TotalRejectedHandpans =
                handpans.Count(x => x.Stage == ProductionStage.Rejected),

            InProductionHandpans =
                handpans.Count(x =>
                    x.Stage != ProductionStage.FinishedWarehouse &&
                    x.Stage != ProductionStage.Rejected)
        };
    }
}