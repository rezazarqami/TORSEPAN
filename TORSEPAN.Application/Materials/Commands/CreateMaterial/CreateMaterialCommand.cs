using MediatR;

namespace TORSEPAN.Application.Materials.Commands.CreateMaterial;

public sealed record CreateMaterialCommand(string Name, int Category = 3, int InitialQuantity = 0,
    int InitialTopBowlQuantity = 0, int InitialBottomBowlQuantity = 0) : IRequest<Guid>;
