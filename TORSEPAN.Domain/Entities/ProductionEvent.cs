using TORSEPAN.Domain.Common;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Domain.Entities;

public class ProductionEvent : Entity
{
    public Guid UserId { get; private set; }

    public Guid? BowlId { get; private set; }

    public Guid? AssemblyId { get; private set; }

    public Guid? HandpanId { get; private set; }

    public ProductionAction Action { get; private set; }

    public EventResult Result { get; private set; }

    public OperationDuration? Duration { get; private set; }

    public DateTime EventDate { get; private set; }

    public string? Description { get; private set; }

    // Navigation Properties
    public User User { get; private set; } = null!;

    public Bowl? Bowl { get; private set; }

    public HandpanAssembly? Assembly { get; private set; }

    public Handpan? Handpan { get; private set; }

    private ProductionEvent()
    {
    }

    public ProductionEvent(
        Guid userId,
        ProductionAction action,
        EventResult result,
        Guid? bowlId = null,
        Guid? assemblyId = null,
        Guid? handpanId = null,
        OperationDuration? duration = null,
        string? description = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User is required.");

        int owners =
            (bowlId.HasValue ? 1 : 0) +
            (assemblyId.HasValue ? 1 : 0) +
            (handpanId.HasValue ? 1 : 0);

        if (owners != 1)
            throw new ArgumentException("Exactly one event owner must be specified.");

        UserId = userId;
        Action = action;
        Result = result;

        BowlId = bowlId;
        AssemblyId = assemblyId;
        HandpanId = handpanId;

        Duration = duration;

        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();

        EventDate = DateTime.UtcNow;
    }
}