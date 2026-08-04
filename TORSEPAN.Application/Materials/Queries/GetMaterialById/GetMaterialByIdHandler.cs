using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Materials.Queries.GetMaterialById;

public sealed class GetMaterialByIdHandler
    : IRequestHandler<GetMaterialByIdQuery, MaterialDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMaterialByIdHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MaterialDto?> Handle(
        GetMaterialByIdQuery request,
        CancellationToken cancellationToken)
    {
        var material = await _unitOfWork.Materials.GetByIdAsync(request.Id);

        if (material is null)
            return null;

        return new MaterialDto
        {
            Id = material.Id,
            Name = material.Name
        };
    }
}