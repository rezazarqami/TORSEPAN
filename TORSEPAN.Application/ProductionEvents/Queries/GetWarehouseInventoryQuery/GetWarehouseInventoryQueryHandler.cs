using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Enums;

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

        return handpans.Select(x =>
        {
            var events = x.ProductionEvents
                .Concat(x.Assembly.TopBowl.ProductionEvents)
                .Concat(x.Assembly.BottomBowl.ProductionEvents)
                .Where(e => e.Action is ProductionAction.Dimple
                    or ProductionAction.Shape
                    or ProductionAction.Tune
                    or ProductionAction.FineTune
                    or ProductionAction.QualityCheck)
                .GroupBy(e => e.Action)
                .ToDictionary(g => g.Key, g => g.ToList());

            var operations = new[]
            {
                (ProductionAction.Dimple, "دیمپل"),
                (ProductionAction.Shape, "شیپ"),
                (ProductionAction.Tune, "تیون"),
                (ProductionAction.FineTune, "فاین تیون"),
                (ProductionAction.QualityCheck, "QC")
            }.Select(operation =>
            {
                events.TryGetValue(operation.Item1, out var actionEvents);
                actionEvents ??= [];
                var latest = actionEvents.OrderByDescending(e => e.EventDate).FirstOrDefault();
                var names = actionEvents
                    .Select(e => string.IsNullOrWhiteSpace(e.User.FullName) ? e.User.UserName : e.User.FullName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct()
                    .ToList();

                return new WarehouseOperationResponse
                {
                    Operation = operation.Item2,
                    PerformedBy = names.Count == 0 ? "ثبت نشده" : string.Join("، ", names),
                    PerformedAt = latest?.EventDate ?? default
                };
            }).ToList();

            var packagingItems = x.ProductionEvents
                .Where(e => e.Action == ProductionAction.Packaging &&
                    e.Description.StartsWith("PACKAGING_ITEMS:"))
                .OrderByDescending(e => e.EventDate)
                .SelectMany(e => e.Description["PACKAGING_ITEMS:".Length..]
                    .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct()
                .ToList();

            return new GetWarehouseInventoryResponse
            {
                HandpanId = x.Id,
                SerialNumber = x.SerialNumber,
                Stage = x.Stage.ToString(),
                TopBowlCode = x.Assembly.TopBowl.ProductionCode,
                BottomBowlCode = x.Assembly.BottomBowl.ProductionCode,
                MaterialName = x.Assembly.TopBowl.Material.Name,
                ScaleName = x.Scale?.Name ?? "تعیین نشده",
                CreatedAt = x.CreatedAt,
                WarehouseEntryDate = x.UpdatedAt,
                Operations = operations,
                PackagingItems = packagingItems
            };
        }).ToList();
    }
}
