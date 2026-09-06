using MediatR;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
namespace TORSEPAN.Application.Sales;
public sealed class SellHandpanCommandHandler(IUnitOfWork unitOfWork, IUserContext userContext) : IRequestHandler<SellHandpanCommand>
{
    public async Task Handle(SellHandpanCommand request, CancellationToken cancellationToken)
    {
        var item=await unitOfWork.Handpans.GetByIdAsync(request.HandpanId)??throw new KeyNotFoundException();
        var userId=userContext.UserId??throw new UnauthorizedAccessException(); item.Sell(request.BuyerName,request.Price,request.Destination,userId);
        unitOfWork.Handpans.Update(item);
        var details = new List<string>();
        if(!string.IsNullOrWhiteSpace(item.BuyerName)) details.Add($"خریدار: {item.BuyerName}");
        if(request.Price.HasValue) details.Add($"قیمت: {request.Price.Value:N0}");
        if(!string.IsNullOrWhiteSpace(item.SaleDestination)) details.Add($"مقصد: {item.SaleDestination}");
        await unitOfWork.ProductionEvents.AddAsync(new ProductionEvent(item.Id,item.AssemblyId,null,userId,ProductionAction.Sale,EventResult.Completed,null,details.Count==0?"فروش از انبار":string.Join(" | ",details)));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
