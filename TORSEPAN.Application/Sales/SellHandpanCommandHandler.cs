Exit code: 0
Wall time: 0.6 seconds
Output:
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
        await unitOfWork.ProductionEvents.AddAsync(new ProductionEvent(item.Id,item.AssemblyId,null,userId,ProductionAction.Sale,EventResult.Completed,null,$"خریدار: {item.BuyerName} | قیمت: {request.Price} | مقصد: {request.Destination}"));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

