using MediatR;
using TORSEPAN.Application.Materials.Queries.GetAllMaterials;

namespace TORSEPAN.Application.Materials.Queries.GetMaterialById;

public sealed record GetMaterialByIdQuery(Guid Id)
    : IRequest<MaterialDto?>;