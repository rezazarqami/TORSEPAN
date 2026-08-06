using MediatR;

using TORSEPAN.Application.Common.Interfaces;

namespace TORSEPAN.Application.Materials.Queries.GetAllMaterials;

public sealed record GetAllMaterialsQuery
    : IRequest<IReadOnlyList<MaterialDto>>, IAllowAnonymousRequest;
