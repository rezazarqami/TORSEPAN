using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Handpans.Commands.RejectHandpan;

public sealed class RejectHandpanCommandHandler
    : IRequestHandler<RejectHandpanCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public RejectHandpanCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(
        RejectHandpanCommand request,
        CancellationToken cancellationToken)
    {
        var handpan = await _unitOfWork.Handpans.GetByIdAsync(request.HandpanId);

        if (handpan is null)
            throw new InvalidOperationException("Handpan not found.");

        handpan.Reject();

        _unitOfWork.Handpans.Update(handpan);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}