using MediatR;

namespace TORSEPAN.Application.Materials.Commands.CreateMaterial;

public sealed record CreateMaterialCommand(string Name, int Category = 3, int InitialQuantity = 0) : IRequest<Guid>;
