using MediatR;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using System.Text.Json;

namespace TORSEPAN.Application.Sales;

public sealed class ShipExportBowlCommandHandler(IUnitOfWork unitOfWork, IUserContext userContext)
    : IRequestHandler<ShipExportBowlCommand>
{
    public async Task Handle(ShipExportBowlCommand request, CancellationToken cancellationToken)
    {
        var bowl = await unitOfWork.Bowls.GetByIdAsync(request.BowlId)
            ?? throw new KeyNotFoundException("Export bowl was not found.");

        if (bowl.Stage != ProductionStage.ExportWarehouse)
            throw new InvalidOperationException("Only export warehouse bowls can be shipped.");

        if (userContext.UserId is not Guid userId)
            throw new UnauthorizedAccessException();

        await ShipAsync(bowl, request.BuyerName, request.PartyId, request.Destination,
            request.ShippingMethod, request.IsSettled, userId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ShipAsync(Bowl bowl, string? buyerName, Guid? partyId, string? destination,
        string? shippingMethod, bool? isSettled, Guid userId)
    {
        bowl.ChangeStage(ProductionStage.Sold);
        bowl.CompleteProduction();
        unitOfWork.Bowls.Update(bowl);
        await unitOfWork.ProductionEvents.AddAsync(new ProductionEvent(
            null, null, bowl.Id, userId, ProductionAction.Sale,
            EventResult.Completed, null, ExportSaleMetadata.Encode(
                buyerName, partyId, destination, shippingMethod, isSettled)));
    }
}

public sealed class ShipExportBowlsCommandHandler(IUnitOfWork unitOfWork, IUserContext userContext)
    : IRequestHandler<ShipExportBowlsCommand>
{
    public async Task Handle(ShipExportBowlsCommand request, CancellationToken cancellationToken)
    {
        var ids = request.BowlIds.Distinct().ToArray();
        if (ids.Length is < 1 or > 200)
            throw new InvalidOperationException("Between 1 and 200 export bowls can be shipped together.");
        if (userContext.UserId is not Guid userId) throw new UnauthorizedAccessException();

        var bowls = (await unitOfWork.Bowls.FindAsync(x => ids.Contains(x.Id))).ToList();
        if (bowls.Count != ids.Length || bowls.Any(x => x.Stage != ProductionStage.ExportWarehouse))
            throw new InvalidOperationException("All selected bowls must be available in the export warehouse.");

        foreach (var bowl in bowls)
        {
            bowl.ChangeStage(ProductionStage.Sold);
            bowl.CompleteProduction();
            unitOfWork.Bowls.Update(bowl);
            await unitOfWork.ProductionEvents.AddAsync(new ProductionEvent(
                null, null, bowl.Id, userId, ProductionAction.Sale, EventResult.Completed, null,
                ExportSaleMetadata.Encode(request.BuyerName, request.PartyId, request.Destination,
                    request.ShippingMethod, request.IsSettled)));
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed record ExportSaleDetails(string? BuyerName, Guid? PartyId, string? Destination,
    string? ShippingMethod, bool? IsSettled);

public static class ExportSaleMetadata
{
    public const string Prefix = "EXPORT_SALE:";
    public static string Encode(string? buyerName, Guid? partyId, string? destination,
        string? shippingMethod, bool? isSettled) => Prefix + JsonSerializer.Serialize(new ExportSaleDetails(
            Clean(buyerName), partyId, Clean(destination), Clean(shippingMethod), isSettled));

    public static ExportSaleDetails? Decode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith(Prefix, StringComparison.Ordinal)) return null;
        try { return JsonSerializer.Deserialize<ExportSaleDetails>(value[Prefix.Length..]); }
        catch (JsonException) { return null; }
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
