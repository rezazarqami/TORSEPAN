using MediatR;

namespace TORSEPAN.Application.Materials.Queries.GetAllMaterials;

public sealed record GetAllMaterialsQuery : IRequest<IReadOnlyList<MaterialDto>>;