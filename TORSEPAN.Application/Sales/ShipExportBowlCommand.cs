Exit code: 0
Wall time: 0.5 seconds
Output:
using MediatR;

namespace TORSEPAN.Application.Sales;

public sealed record ShipExportBowlCommand(Guid BowlId) : IRequest;

