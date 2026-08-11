using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Queries.GetExportWarehouse;

public sealed class GetExportWarehouseQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetExportWarehouseQuery, IReadOnlyList<ExportWarehouseItemDto>>
{
    public async Task<IReadOnlyList<ExportWarehouseItemDto>> Handle(GetExportWarehouseQuery request, CancellationToken cancellationToken)
    {
        var bowls = (await unitOfWork.Bowls.FindAsync(x => x.Stage == ProductionStage.ExportWarehouse)).ToList();
        var materials = (await unitOfWork.Materials.GetAllAsync()).ToDictionary(x => x.Id, x => x.Name);
        return bowls.OrderByDescending(x => x.ProductionCode)
            .Select(x => new ExportWarehouseItemDto(x.Id, x.ProductionCode, (int)x.BowlType,
                materials.GetValueOrDefault(x.MaterialId, "—"))).ToList();
    }
}
