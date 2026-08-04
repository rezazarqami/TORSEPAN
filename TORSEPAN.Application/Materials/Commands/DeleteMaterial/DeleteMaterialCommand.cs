using MediatR;

namespace TORSEPAN.Application.Materials.Commands.DeleteMaterial;

public sealed record DeleteMaterialCommand(Guid Id) : IRequest;