using MediatR;
using TORSEPAN.Application.Interfaces;
namespace TORSEPAN.Application.Sales;
public sealed class GetSalesQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetSalesQuery,IReadOnlyList<SaleItemResponse>>
{
 public async Task<IReadOnlyList<SaleItemResponse>> Handle(GetSalesQuery request,CancellationToken ct){var items=await unitOfWork.Handpans.GetSoldInventoryAsync();var users=await unitOfWork.Users.GetAllAsync();return items.Select(x=>{var u=users.FirstOrDefault(y=>y.Id==x.SoldByUserId);return new SaleItemResponse{HandpanId=x.Id,SerialNumber=x.SerialNumber,BuyerName=x.BuyerName??"",SoldAt=x.SoldAt??x.UpdatedAt??x.CreatedAt,SoldBy=u is null?"ثبت نشده":string.IsNullOrWhiteSpace(u.FullName)?u.UserName:u.FullName,MaterialName=x.Assembly.TopBowl.Material.Name,ScaleName=x.Scale?.Name??"تعیین نشده",TopBowlCode=x.Assembly.TopBowl.ProductionCode,BottomBowlCode=x.Assembly.BottomBowl.ProductionCode};}).ToList();}
}

