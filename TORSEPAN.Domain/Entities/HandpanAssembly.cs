using TORSEPAN.Domain.Common;

namespace TORSEPAN.Domain.Entities;

public sealed class HandpanAssembly : Entity
{
    public Guid TopBowlId { get; private set; }

    public Guid BottomBowlId { get; private set; }

    public DateTime AssemblyDate { get; private set; }

    public Bowl TopBowl { get; private set; } = null!;

    public Bowl BottomBowl { get; private set; } = null!;

    public Handpan Handpan { get; private set; } = null!;

    public ICollection<ProductionEvent> ProductionEvents { get; private set; } = new List<ProductionEvent>();

    private HandpanAssembly()
    {
    }

    public HandpanAssembly(
        Guid topBowlId,
        Guid bottomBowlId)
    {
        if (topBowlId == Guid.Empty)
            throw new ArgumentException(nameof(topBowlId));

        if (bottomBowlId == Guid.Empty)
            throw new ArgumentException(nameof(bottomBowlId));

        if (topBowlId == bottomBowlId)
            throw new InvalidOperationException("Top and bottom bowls cannot be the same.");

        TopBowlId = topBowlId;
        BottomBowlId = bottomBowlId;
        AssemblyDate = DateTime.UtcNow;
    }

    public Handpan CreateHandpan(string serialNumber)
    {
        if (Handpan is not null)
            throw new InvalidOperationException(
                "A handpan has already been created for this assembly.");

        if (string.IsNullOrWhiteSpace(serialNumber))
            throw new ArgumentException(
                "Serial number is required.",
                nameof(serialNumber));

        Handpan = new Handpan(Id, serialNumber);

        return Handpan;
    }
}