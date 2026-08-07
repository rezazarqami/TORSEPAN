using TORSEPAN.Domain.Common;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Domain.Entities;

public class ProductionEvent : Entity
{
    private ProductionEvent()
    {
    }

    // Used by ChangeProductionStageCommand
    public ProductionEvent(
        Guid userId,
        ProductionAction action,
        EventResult result,
        Guid handpanId,
        string? description)
        : this(
            handpanId,
            null,
            null,
            userId,
            action,
            result,
            null,
            description)
    {
    }

    // Used by CreateProductionEventCommand
    public ProductionEvent(
        Guid? handpanId,
        Guid? assemblyId,
        Guid? bowlId,
        Guid userId,
        ProductionAction action,
        EventResult result,
        OperationDuration? duration,
        string? description)
    {
        Id = Guid.NewGuid();

        HandpanId = handpanId;
        AssemblyId = assemblyId;
        BowlId = bowlId;

        UserId = userId;

        Action = action;
        Result = result;
        Duration = duration;

        Description = description ?? string.Empty;
        EventDate = DateTime.UtcNow;
    }

    public Guid? HandpanId { get; private set; }

    public Handpan? Handpan { get; private set; }

    public Guid? AssemblyId { get; private set; }

    public HandpanAssembly? Assembly { get; private set; }

    public Guid? BowlId { get; private set; }

    public Bowl? Bowl { get; private set; }

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public ProductionAction Action { get; private set; }

    public EventResult Result { get; private set; }

    public OperationDuration? Duration { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public DateTime EventDate { get; private set; }
}
