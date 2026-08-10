using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Materials.Commands.AdjustBowlStock;

public sealed class AdjustBowlStockHandler : IRequestHandler<AdjustBowlStockCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    public AdjustBowlStockHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task Handle(AdjustBowlStockCommand request, CancellationToken cancellationToken)
    {
        var material = await _unitOfWork.Materials.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException("Material not found.");

        if ((int)material.Category != 4)
            throw new InvalidOperationException("This material is not a bowl material.");

        if (request.SetAbsolute)
            material.SetBowlStock(request.TopQuantity, request.BottomQuantity);
        else
            material.AddBowlStock(request.TopQuantity, request.BottomQuantity);

        _unitOfWork.Materials.Update(material);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
