using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionCountByStatus;

public sealed class GetProductionCountByStatusQueryHandler
    : IRequestHandler<GetProductionCountByStatusQuery, GetProductionCountByStatusResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProductionCountByStatusQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetProductionCountByStatusResponse> Handle(
        GetProductionCountByStatusQuery request,
        CancellationToken cancellationToken)
    {
        var inProgress = await _unitOfWork.Handpans.GetByStatusAsync(ProductionStatus.InProgress);
        var completed = await _unitOfWork.Handpans.GetByStatusAsync(ProductionStatus.Completed);
        var rejected = await _unitOfWork.Handpans.GetByStatusAsync(ProductionStatus.Rejected);
        var readyForPackaging = await _unitOfWork.Handpans.GetReadyForPackagingAsync();
        var warehouse = await _unitOfWork.Handpans.GetWarehouseInventoryAsync();

        return new GetProductionCountByStatusResponse
        {
            InProduction = inProgress.Count(),
            Finished = completed.Count(),
            Rejected = rejected.Count(),
            ReadyForPackaging = readyForPackaging.Count(),
            Warehouse = warehouse.Count()
        };
    }
}