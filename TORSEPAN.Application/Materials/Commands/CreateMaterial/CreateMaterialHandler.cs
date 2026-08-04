using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Application.Materials.Commands.CreateMaterial;

public sealed class CreateMaterialHandler
    : IRequestHandler<CreateMaterialCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateMaterialHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(
        CreateMaterialCommand request,
        CancellationToken cancellationToken)
    {
        var material = new Material(request.Name);

        await _unitOfWork.Materials.AddAsync(material);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return material.Id;
    }
}