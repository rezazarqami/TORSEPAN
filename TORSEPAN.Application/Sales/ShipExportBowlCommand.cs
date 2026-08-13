using MediatR;

namespace TORSEPAN.Application.Sales;

public sealed record ShipExportBowlCommand(Guid BowlId) : IRequest;
