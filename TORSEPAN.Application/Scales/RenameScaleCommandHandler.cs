using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Scales;

public sealed class RenameScaleCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<RenameScaleCommand>
{
    public async Task Handle(RenameScaleCommand request, CancellationToken cancellationToken)
    {
        var scale = await unitOfWork.Scales.GetByIdAsync(request.Id)
            ?? throw new InvalidOperationException("اسکیل پیدا نشد.");
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("نام اسکیل الزامی است.");
        if ((await unitOfWork.Scales.FindAsync(x => x.Name == name && x.Id != request.Id)).Any())
            throw new InvalidOperationException("اسکیلی با این نام از قبل وجود دارد.");
        scale.Rename(name);
        unitOfWork.Scales.Update(scale);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
