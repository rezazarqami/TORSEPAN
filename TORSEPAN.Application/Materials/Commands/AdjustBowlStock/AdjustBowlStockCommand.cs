using MediatR;

namespace TORSEPAN.Application.Materials.Commands.AdjustBowlStock;

public sealed record AdjustBowlStockCommand(
    Guid Id,
    int TopQuantity,
    int BottomQuantity,
    bool SetAbsolute) : IRequest;
