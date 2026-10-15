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
        var usage = ScaleUsage.All;
        if (usage == ScaleUsage.None || (usage & ~ScaleUsage.All) != 0)
            throw new InvalidOperationException("دسته‌بندی Scale معتبر نیست.");
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            throw new InvalidOperationException("نام اسکیل باید بین ۱ و ۱۰۰ کاراکتر باشد.");
        var existing = (await _unitOfWork.Scales.GetAllAsync())
            .Where(x => Scale.NameKey(x.Name) == Scale.NameKey(name))
            .OrderByDescending(x => x.IsActive).ThenBy(x => x.Id).FirstOrDefault();
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
