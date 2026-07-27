using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionReport;

public sealed class GetProductionReportQueryHandler
    : IRequestHandler<GetProductionReportQuery, GetProductionReportResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProductionReportQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetProductionReportResponse> Handle(
        GetProductionReportQuery request,
        CancellationToken cancellationToken)
    {
        var handpans = await _unitOfWork.Handpans.GetAllAsync();

        var stages = handpans
            .GroupBy(x => x.Stage.ToString())
            .Select(x => new GetProductionReportResponse.StageItem
            {
                Stage = x.Key,
                Count = x.Count()
            })
            .OrderBy(x => x.Stage)
            .ToList();

        return new GetProductionReportResponse
        {
            TotalHandpans = handpans.Count(),
            TotalStages = stages.Count,
            Stages = stages
        };
    }
}