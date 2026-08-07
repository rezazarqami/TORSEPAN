using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetWarehouseInventory;

public sealed class GetWarehouseInventoryQueryHandler
    : IRequestHandler<GetWarehouseInventoryQuery, IReadOnlyCollection<GetWarehouseInventoryResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetWarehouseInventoryQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<GetWarehouseInventoryResponse>> Handle(
        GetWarehouseInventoryQuery request,
        CancellationToken cancellationToken)
    {
        var handpans = await _unitOfWork.Handpans.GetWarehouseInventoryAsync();

        return handpans
            .Select(x => new GetWarehouseInventoryResponse
            {
                HandpanId = x.Id,
                SerialNumber = x.SerialNumber,
                Stage = x.Stage.ToString(),
                TopBowlCode = x.Assembly.TopBowl.ProductionCode,
                BottomBowlCode = x.Assembly.BottomBowl.ProductionCode,
                MaterialName = x.Assembly.TopBowl.Material.Name,
                ScaleName = x.Scale?.Name ?? "تعیین نشده",
                CreatedAt = x.CreatedAt,
                WarehouseEntryDate = x.UpdatedAt
            })
            .ToList();
    }
}
