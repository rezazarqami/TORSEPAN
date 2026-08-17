using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Scales;

public sealed class CreateScaleCommandHandler : IRequestHandler<CreateScaleCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateScaleCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<Guid> Handle(CreateScaleCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var usage = (ScaleUsage)request.Usage;
        if (usage == ScaleUsage.None || (usage & ~ScaleUsage.All) != 0)
            throw new InvalidOperationException("دسته‌بندی اسکیل معتبر نیست.");
        var existing = (await _unitOfWork.Scales.FindAsync(x => x.Name == name)).FirstOrDefault();
        if (existing is not null)
        {
            existing.Activate();
            existing.AddUsage(usage);
            _unitOfWork.Scales.Update(existing);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return existing.Id;
        }

        var scale = new Scale(name, usage);
        await _unitOfWork.Scales.AddAsync(scale);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return scale.Id;
    }
}
