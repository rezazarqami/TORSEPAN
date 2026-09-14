using MediatR;
namespace TORSEPAN.Application.Sales;
public sealed record SellHandpanCommand(Guid HandpanId, string? BuyerName, string? BuyerPhoneNumber, decimal? Price, string? Destination) : IRequest;
