using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Enums;
namespace TORSEPAN.Application.Scales;
public sealed class DeactivateScaleCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeactivateScaleCommand>
{
    public async Task Handle(DeactivateScaleCommand request, CancellationToken cancellationToken)
    {
        var scale = await unitOfWork.Scales.GetByIdAsync(request.Id) ?? throw new InvalidOperationException("Scale پیدا نشد.");
        var usage = (ScaleUsage)request.Usage;
        if (usage is not (ScaleUsage.TopBowl or ScaleUsage.BottomBowl or ScaleUsage.Handpan or
            ScaleUsage.CustomTopBowl or ScaleUsage.CustomBottomBowl or ScaleUsage.CustomHandpan))
            throw new InvalidOperationException("دسته‌بندی Scale معتبر نیست.");
        scale.RemoveUsage(usage); unitOfWork.Scales.Update(scale);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
