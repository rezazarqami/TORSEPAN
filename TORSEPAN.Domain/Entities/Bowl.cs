using TORSEPAN.Domain.Common;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Domain.Entities;

public class Bowl : Entity
{
    public string ProductionCode { get; private set; } = string.Empty;

    public BowlType BowlType { get; private set; }

    public bool HasNotes { get; private set; }

    public InstrumentType InstrumentType { get; private set; }

    public int? NoteCount { get; private set; }

    public ProductionStatus Status { get; private set; }

    public ProductionStage Stage { get; private set; }

    // Navigation Properties
    public ICollection<ProductionEvent> ProductionEvents { get; private set; } = new List<ProductionEvent>();

    public ICollection<HandpanAssembly> TopAssemblies { get; private set; } = new List<HandpanAssembly>();

    public ICollection<HandpanAssembly> BottomAssemblies { get; private set; } = new List<HandpanAssembly>();

    private Bowl()
    {
    }

    public Bowl(
        string productionCode,
        BowlType bowlType,
        bool hasNotes,
        InstrumentType instrumentType,
        int? noteCount)
    {
        if (string.IsNullOrWhiteSpace(productionCode))
            throw new ArgumentException("Production code is required.");

        if (hasNotes && noteCount is null)
            throw new ArgumentException("Note count is required.");

        if (!hasNotes && noteCount is not null)
            throw new ArgumentException("Note count must be null.");

        ProductionCode = productionCode.Trim();
        BowlType = bowlType;
        HasNotes = hasNotes;
        InstrumentType = instrumentType;
        NoteCount = noteCount;

        Status = ProductionStatus.Created;
        Stage = ProductionStage.Created;
    }

    public void StartProduction()
    {
        Status = ProductionStatus.InProgress;
    }

    public void ChangeStage(ProductionStage stage)
    {
        Stage = stage;
    }

    public void CompleteProduction()
    {
        Status = ProductionStatus.Completed;
    }

    public void Reject()
    {
        Status = ProductionStatus.Rejected;
    }
}