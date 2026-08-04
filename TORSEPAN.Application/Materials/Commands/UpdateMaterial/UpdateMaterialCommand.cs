using MediatR;

namespace TORSEPAN.Application.Materials.Commands.UpdateMaterial;

public sealed record UpdateMaterialCommand(
    Guid Id,
    string Name) : IRequest;