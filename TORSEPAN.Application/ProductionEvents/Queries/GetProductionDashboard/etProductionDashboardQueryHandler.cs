using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionDashboard;

public sealed class GetProductionDashboardQueryHandler
    : IRequestHandler<GetProductionDashboardQuery, GetProductionDashboardResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    public GetProductionDashboardQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<GetProductionDashboardResponse> Handle(
        GetProductionDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var bowls = (await _unitOfWork.Bowls.GetAllAsync()).ToList();
        var handpans = (await _unitOfWork.Handpans.GetAllAsync()).ToList();
        var finished = handpans.Count(x => x.Stage == ProductionStage.FinishedWarehouse);
        var rejected = handpans.Count(x => x.Stage == ProductionStage.Rejected);

        return new GetProductionDashboardResponse
        {
            TotalHandpans = handpans.Count,
            Finished = finished,
            Rejected = rejected,
            InProduction = handpans.Count - finished - rejected,
            CompletionRate = handpans.Count == 0 ? 0 : Math.Round((double)finished / handpans.Count * 100, 2),
            Queues =
            [
                BowlQueue("آماده دیمپل", ProductionStage.WaitingForDimple),
                BowlQueue("آماده شیپ", ProductionStage.WaitingForShape),
                BowlQueue("آماده تیون", ProductionStage.WaitingForTune),
                BowlQueue("آماده بسته‌بندی صادراتی", ProductionStage.WaitingForExportPackaging),
                HandpanQueue("آماده فاین تیون", ProductionStage.WaitingForFinalTune),
                HandpanQueue("آماده کنترل کیفیت (QC)", ProductionStage.WaitingForQualityControl),
                HandpanQueue("آماده بسته‌بندی", ProductionStage.WaitingForPackaging)
            ]
        };

        ProductionQueueItemResponse BowlQueue(string title, ProductionStage stage) => new()
        {
            Stage = title,
            Codes = bowls.Where(x => x.Stage == stage).Select(x => x.ProductionCode).OrderBy(x => x).ToList()
        };

        ProductionQueueItemResponse HandpanQueue(string title, ProductionStage stage) => new()
        {
            Stage = title,
            Codes = handpans.Where(x => x.Stage == stage).Select(x => x.SerialNumber).OrderBy(x => x).ToList()
        };
    }
}
