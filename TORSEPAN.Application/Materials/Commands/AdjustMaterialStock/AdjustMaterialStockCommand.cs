using MediatR;

namespace TORSEPAN.Application.Materials.Commands.AdjustMaterialStock;

public sealed record AdjustMaterialStockCommand(Guid Id, int Quantity, bool SetAbsolute) : IRequest<int>;
