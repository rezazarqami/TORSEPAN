using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Enums;
namespace TORSEPAN.Application.Scales;
public sealed class DeactivateScaleCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeactivateScaleCommand>
{
    public async Task Handle(DeactivateScaleCommand request, CancellationToken cancellationToken)
    {
        var scale = await unitOfWork.Scales.GetByIdAsync(request.Id) ?? throw new InvalidOperationException("Scale پیدا نشد.");
        scale.Deactivate(); unitOfWork.Scales.Update(scale);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
