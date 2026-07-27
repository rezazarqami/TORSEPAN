using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.ProductionEvents.Commands.CompleteProduction;

public sealed class CompleteProductionCommandHandler
    : IRequestHandler<CompleteProductionCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public CompleteProductionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(
        CompleteProductionCommand request,
        CancellationToken cancellationToken)
    {
        var handpan = await _unitOfWork.Handpans.GetByIdAsync(request.HandpanId);

        if (handpan is null)
            throw new InvalidOperationException("Handpan not found.");

        handpan.CompleteProduction();

        _unitOfWork.Handpans.Update(handpan);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}