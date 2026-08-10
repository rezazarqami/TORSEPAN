using MediatR;
namespace TORSEPAN.Application.Materials.Commands.SetLowStockThreshold;
public sealed record SetLowStockThresholdCommand(Guid Id, int Quantity, int TopQuantity, int BottomQuantity) : IRequest;
