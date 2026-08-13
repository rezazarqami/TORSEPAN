using MediatR;
using TORSEPAN.Application.Interfaces;
namespace TORSEPAN.Application.Materials.Commands.SetLowStockThreshold;
public sealed class SetLowStockThresholdHandler(IUnitOfWork unitOfWork) : IRequestHandler<SetLowStockThresholdCommand>
{
    public async Task Handle(SetLowStockThresholdCommand request, CancellationToken cancellationToken)
    {
        var item = await unitOfWork.Materials.GetByIdAsync(request.Id) ?? throw new KeyNotFoundException();
        item.SetLowStockThresholds(request.Quantity, request.TopQuantity, request.BottomQuantity);
        unitOfWork.Materials.Update(item); await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
