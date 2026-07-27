using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Application.Handpans.Commands.CreateHandpan;

public sealed class CreateHandpanCommandHandler
    : IRequestHandler<CreateHandpanCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateHandpanCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(
        CreateHandpanCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.Handpans
            .GetBySerialNumberAsync(request.SerialNumber);

        if (existing is not null)
            throw new InvalidOperationException(
                $"Handpan '{request.SerialNumber}' already exists.");

        var handpan = new Handpan(
            request.AssemblyId,
            request.SerialNumber);

        await _unitOfWork.Handpans.AddAsync(handpan);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return handpan.Id;
    }
}