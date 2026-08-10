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
        var userId=userContext.UserId??throw new UnauthorizedAccessException(); item.Sell(request.BuyerName,userId);
        unitOfWork.Handpans.Update(item);
        await unitOfWork.ProductionEvents.AddAsync(new ProductionEvent(item.Id,item.AssemblyId,null,userId,ProductionAction.Sale,EventResult.Completed,null,$"خریدار: {item.BuyerName}"));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

