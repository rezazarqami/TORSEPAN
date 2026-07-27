using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionDashboard;

public sealed class GetProductionDashboardQueryHandler
    : IRequestHandler<GetProductionDashboardQuery, GetProductionDashboardResponse>
{
    private readonly IHandpanRepository _handpanRepository;

    public GetProductionDashboardQueryHandler(IHandpanRepository handpanRepository)
    {
        _handpanRepository = handpanRepository;
    }

    public async Task<GetProductionDashboardResponse> Handle(
        GetProductionDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var handpans = await _handpanRepository.GetAllAsync();

        var list = handpans?.ToList() ?? new List<TORSEPAN.Domain.Entities.Handpan>();

        var total = list.Count;

        var finished = list.Count(x => x.Stage == ProductionStage.FinishedWarehouse);

        var rejected = list.Count(x => x.Stage == ProductionStage.Rejected);

        var inProduction = total - finished - rejected;

        return new GetProductionDashboardResponse
        {
            TotalHandpans = total,
            Finished = finished,
            Rejected = rejected,
            InProduction = inProduction,
            CompletionRate = total == 0
                ? 0
                : Math.Round((double)finished / total * 100, 2)
        };
    }
}