using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Sales;

public sealed class GetSalesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetSalesQuery, IReadOnlyList<SaleItemResponse>>
{
    public async Task<IReadOnlyList<SaleItemResponse>> Handle(GetSalesQuery request, CancellationToken ct)
    {
        var users = await unitOfWork.Users.GetAllAsync();
        var results = new List<SaleItemResponse>();

        var handpans = await unitOfWork.Handpans.GetSoldInventoryAsync();
        results.AddRange(handpans.Select(x =>
        {
            var user = users.FirstOrDefault(y => y.Id == x.SoldByUserId);
            return new SaleItemResponse
            {
                HandpanId = x.Id,
                ItemType = "ساز",
                SerialNumber = x.SerialNumber,
                BuyerName = x.BuyerName ?? "",
                Price = x.SalePrice,
                Destination = x.SaleDestination ?? "",
                SoldAt = x.SoldAt ?? x.UpdatedAt ?? x.CreatedAt,
                SoldBy = UserName(user),
                MaterialName = x.Assembly.TopBowl.Material.Name,
                ScaleName = x.Scale?.Name ?? "تعیین نشده",
                TopBowlCode = x.Assembly.TopBowl.ProductionCode,
                BottomBowlCode = x.Assembly.BottomBowl.ProductionCode
            };
        }));

        var soldBowls = (await unitOfWork.Bowls.FindAsync(x => x.Stage == ProductionStage.Sold)).ToList();
        if (soldBowls.Count > 0)
        {
            var materials = (await unitOfWork.Materials.GetAllAsync()).ToDictionary(x => x.Id, x => x.Name);
            var saleEvents = await unitOfWork.ProductionEvents.GetReportAsync(
                null, null, null, ProductionAction.Sale, EventResult.Completed);

            results.AddRange(soldBowls.Select(bowl =>
            {
                var saleEvent = saleEvents.FirstOrDefault(x => x.BowlId == bowl.Id);
                var user = saleEvent is null ? null : users.FirstOrDefault(x => x.Id == saleEvent.UserId);
                return new SaleItemResponse
                {
                    HandpanId = bowl.Id,
                    IsBowl = true,
                    ItemType = "کاسه صادراتی",
                    SerialNumber = bowl.ProductionCode,
                    BuyerName = "ارسال صادراتی",
                    SoldAt = saleEvent?.EventDate ?? DateTime.MinValue,
                    SoldBy = UserName(user),
                    MaterialName = materials.GetValueOrDefault(bowl.MaterialId, "—"),
                    ScaleName = "—",
                    TopBowlCode = bowl.BowlType == BowlType.Top ? bowl.ProductionCode : "—",
                    BottomBowlCode = bowl.BowlType == BowlType.Bottom ? bowl.ProductionCode : "—"
                };
            }));
        }

        return results.OrderByDescending(x => x.SoldAt).ToList();
    }

    private static string UserName(TORSEPAN.Domain.Entities.User? user) => user is null
        ? "ثبت نشده"
        : string.IsNullOrWhiteSpace(user.FullName) ? user.UserName : user.FullName;
}
