using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.ProductionEvents.Commands.MoveToWarehouse;

public sealed class MoveToWarehouseCommandHandler
    : IRequestHandler<MoveToWarehouseCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public MoveToWarehouseCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(
        MoveToWarehouseCommand request,
        CancellationToken cancellationToken)
    {
        var handpan = await _unitOfWork.Handpans.GetByIdAsync(request.HandpanId);

        if (handpan is null)
            throw new InvalidOperationException("Handpan not found.");

        handpan.ChangeStage(ProductionStage.FinishedWarehouse);

        _unitOfWork.Handpans.Update(handpan);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}