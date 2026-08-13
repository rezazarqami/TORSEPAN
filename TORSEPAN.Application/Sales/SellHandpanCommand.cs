Exit code: 0
Wall time: 0.6 seconds
Output:
using MediatR;
namespace TORSEPAN.Application.Sales;
public sealed record SellHandpanCommand(Guid HandpanId, string BuyerName, decimal Price, string Destination) : IRequest;

