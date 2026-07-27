using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Production;

namespace TORSEPAN.Application.Production.Commands.RegisterProductionOperation;

public sealed class RegisterProductionOperationCommandHandler
    : IRequestHandler<RegisterProductionOperationCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductionEngine _productionEngine;

    public RegisterProductionOperationCommandHandler(
        IUnitOfWork unitOfWork,
        IProductionEngine productionEngine)
    {
        _unitOfWork = unitOfWork;
        _productionEngine = productionEngine;
    }

    public async Task<Guid> Handle(
        RegisterProductionOperationCommand request,
        CancellationToken cancellationToken)
    {
        var handpan = await _unitOfWork.Handpans
            .GetForUpdateBySerialNumberAsync(request.SerialNumber);

        if (handpan is null)
            throw new InvalidOperationException(
                $"Handpan '{request.SerialNumber}' was not found.");

        var transition = _productionEngine.GetTransition(handpan.Stage);

        if (transition is null)
            throw new InvalidOperationException(
                $"No transition is defined for stage '{handpan.Stage}'.");

        handpan.RegisterProductionOperation(
            transition,
            request.UserId,
            request.Result,
            request.Duration,
            request.Description);

        _productionEngine.MoveTo(handpan, transition.NextStage);

        _unitOfWork.Handpans.Update(handpan);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return handpan.Id;
    }
}