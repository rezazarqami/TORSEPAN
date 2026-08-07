using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Application.Scales;

public sealed class CreateScaleCommandHandler : IRequestHandler<CreateScaleCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateScaleCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<Guid> Handle(CreateScaleCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if ((await _unitOfWork.Scales.FindAsync(x => x.Name == name)).Any())
            throw new InvalidOperationException("این اسکیل قبلاً ثبت شده است.");

        var scale = new Scale(name);
        await _unitOfWork.Scales.AddAsync(scale);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return scale.Id;
    }
}
