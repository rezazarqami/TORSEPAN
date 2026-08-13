Exit code: 0
Wall time: 0.6 seconds
Output:
using TORSEPAN.Domain.Common;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Domain.Production;

namespace TORSEPAN.Domain.Entities;

public class Handpan : Entity
{
    private readonly List<ProductionEvent> _productionEvents = new();

    private Handpan()
    {
    }

    public Handpan(Guid assemblyId, string serialNumber)
        : this(assemblyId, serialNumber, null)
    {
    }

    public Handpan(Guid assemblyId, string serialNumber, Guid? scaleId)
    {
        Id = Guid.NewGuid();
        AssemblyId = assemblyId;
        SerialNumber = serialNumber;
        ScaleId = scaleId;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid AssemblyId { get; private set; }

    public HandpanAssembly Assembly { get; private set; } = null!;

    public string SerialNumber { get; private set; } = string.Empty;

    public Guid? ScaleId { get; private set; }

    public Scale? Scale { get; private set; }

    public ProductionStatus Status { get; private set; }

    public ProductionStage Stage { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }
    public string? BuyerName { get; private set; }
    public DateTime? SoldAt { get; private set; }
    public Guid? SoldByUserId { get; private set; }
    public decimal? SalePrice { get; private set; }
    public string? SaleDestination { get; private set; }

    public void Sell(string buyerName, decimal price, string destination, Guid soldByUserId)
    {
        if (Stage != ProductionStage.FinishedWarehouse) throw new InvalidOperationException("Handpan is not in warehouse.");
        if (string.IsNullOrWhiteSpace(buyerName)) throw new ArgumentException("Buyer name is required.");
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
        if (string.IsNullOrWhiteSpace(destination)) throw new ArgumentException("Destination is required.");
        BuyerName = buyerName.Trim(); SalePrice = price; SaleDestination = destination.Trim(); SoldByUserId = soldByUserId; SoldAt = DateTime.UtcNow;
        Stage = ProductionStage.Sold; UpdatedAt = SoldAt;
    }

    public IReadOnlyCollection<ProductionEvent> ProductionEvents => _productionEvents;

    public void RegisterProductionOperation(
        ProductionTransition transition,
        Guid userId,
        EventResult result,
        OperationDuration? duration,
        string description)
    {
        if (!Enum.TryParse<ProductionAction>(
            transition.ToString(),
            true,
            out var action))
        {
            action = default;
        }

        _productionEvents.Add(
            new ProductionEvent(
                handpanId: Id,
                assemblyId: AssemblyId,
                bowlId: null,
                userId: userId,
                action: action,
                result: result,
                duration: duration,
                description: description));

        UpdatedAt = DateTime.UtcNow;
    }

    public void CompleteProduction()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStage(ProductionStage stage)
    {
        Stage = stage;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(ProductionStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }
}

