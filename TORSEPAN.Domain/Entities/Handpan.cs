using TORSEPAN.Domain.Common;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Domain.Production;

namespace TORSEPAN.Domain.Entities;

public class Handpan : Entity
{
    public Guid AssemblyId { get; private set; }

    public string SerialNumber { get; private set; } = string.Empty;

    public new DateTime CreatedAt { get; private set; }

    public ProductionStatus Status { get; private set; }

    public ProductionStage Stage { get; private set; }

    // Navigation
    public HandpanAssembly Assembly { get; private set; } = null!;

    public ICollection<ProductionEvent> ProductionEvents { get; private set; } = new List<ProductionEvent>();

    private Handpan()
    {
    }

    public Handpan(Guid assemblyId, string serialNumber)
    {
        if (assemblyId == Guid.Empty)
            throw new ArgumentException("Assembly is required.");

        if (string.IsNullOrWhiteSpace(serialNumber))
            throw new ArgumentException("Serial number is required.");

        AssemblyId = assemblyId;
        SerialNumber = serialNumber.Trim();

        CreatedAt = DateTime.UtcNow;

        Status = ProductionStatus.InProgress;

        Stage = ProductionStage.Created;
    }

    public void RegisterProductionOperation(
        ProductionTransition transition,
        Guid userId,
        EventResult result,
        OperationDuration? duration,
        string? description)
    {
        ArgumentNullException.ThrowIfNull(transition);

        if (transition.CurrentStage != Stage)
            throw new InvalidOperationException(
                $"Current stage is '{Stage}' but transition expects '{transition.CurrentStage}'.");

        var productionEvent = new ProductionEvent(
            userId: userId,
            action: transition.Action,
            result: result,
            handpanId: Id,
            duration: duration,
            description: description);

        ProductionEvents.Add(productionEvent);

        Stage = transition.NextStage;

        MarkUpdated();
    }

    public void ChangeStage(ProductionStage stage)
    {
        Stage = stage;

        MarkUpdated();
    }

    public void CompleteProduction()
    {
        Status = ProductionStatus.Completed;
        Stage = ProductionStage.FinishedWarehouse;

        MarkUpdated();
    }

    public void Reject()
    {
        Status = ProductionStatus.Rejected;

        MarkUpdated();
    }

    public void AddProductionEvent(ProductionEvent productionEvent)
    {
        ArgumentNullException.ThrowIfNull(productionEvent);

        ProductionEvents.Add(productionEvent);

        MarkUpdated();
    }
}