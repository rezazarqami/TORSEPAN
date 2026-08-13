using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Materials.Queries.GetAllMaterials;

public sealed class GetAllMaterialsHandler
    : IRequestHandler<GetAllMaterialsQuery, IReadOnlyList<MaterialDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllMaterialsHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<MaterialDto>> Handle(
        GetAllMaterialsQuery request,
        CancellationToken cancellationToken)
    {
        var materials = await _unitOfWork.Materials.GetAllAsync();

        return materials
            .Select(x => new MaterialDto
            {
                Id = x.Id,
                Name = x.Name,
                Category = (int)x.Category,
                Quantity = x.Quantity,
                TopBowlQuantity = x.TopBowlQuantity,
                BottomBowlQuantity = x.BottomBowlQuantity,
                LowStockThreshold = x.LowStockThreshold,
                TopBowlLowStockThreshold = x.TopBowlLowStockThreshold,
                BottomBowlLowStockThreshold = x.BottomBowlLowStockThreshold,
                TopBowlCodeTemplate = x.TopBowlCodeTemplate,
                BottomBowlCodeTemplate = x.BottomBowlCodeTemplate
            })
            .ToList();
    }
}
