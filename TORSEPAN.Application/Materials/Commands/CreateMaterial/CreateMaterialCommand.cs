using MediatR;

namespace TORSEPAN.Application.Materials.Commands.CreateMaterial;

public sealed record CreateMaterialCommand(string Name) : IRequest<Guid>;