using MediatR;

namespace TORSEPAN.Application.Sales;

public sealed record ShipExportBowlCommand(
    Guid BowlId,
    string? BuyerName = null,
    Guid? PartyId = null,
    string? Destination = null,
    string? ShippingMethod = null,
    bool? IsSettled = null) : IRequest;

public sealed record ShipExportBowlsCommand(
    IReadOnlyCollection<Guid> BowlIds,
    string? BuyerName = null,
    Guid? PartyId = null,
    string? Destination = null,
    string? ShippingMethod = null,
    bool? IsSettled = null) : IRequest;
