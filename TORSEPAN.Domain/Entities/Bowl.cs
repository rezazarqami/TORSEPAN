using TORSEPAN.Domain.Common;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Domain.Entities;

public class Bowl : Entity
{
    public string ProductionCode { get; private set; } = string.Empty;
    public BowlType BowlType { get; private set; }
    public bool HasNotes { get; private set; }
    public InstrumentType InstrumentType { get; private set; }
    public Guid MaterialId { get; private set; }
    public ProductionStatus Status { get; private set; }
    public ProductionStage Stage { get; private set; }

    public Material Material { get; private set; } = null!;

    public ICollection<ProductionEvent> ProductionEvents { get; private set; } = new List<ProductionEvent>();
    public ICollection<HandpanAssembly> TopAssemblies { get; private set; } = new List<HandpanAssembly>();
    public ICollection<HandpanAssembly> BottomAssemblies { get; private set; } = new List<HandpanAssembly>();

    private Bowl(){}

    public Bowl(string productionCode, BowlType bowlType, bool hasNotes, InstrumentType instrumentType, Guid materialId)
    {
        if(string.IsNullOrWhiteSpace(productionCode))
            throw new ArgumentException("Production code is required.");

        if(bowlType == BowlType.Top)
            hasNotes = true;

        ProductionCode = productionCode.Trim();
        BowlType = bowlType;
        HasNotes = hasNotes;
        InstrumentType = instrumentType;
        MaterialId = materialId;
        Status = ProductionStatus.Waiting;
        Stage = ProductionStage.WaitingForDimple;
    }

    public void StartProduction()=> Status=ProductionStatus.InProgress;
    public void MarkAsWaiting()=> Status=ProductionStatus.Waiting;
    public void ChangeStage(ProductionStage stage)=> Stage=stage;
    public void CompleteProduction()=> Status=ProductionStatus.Completed;
    public void Reject()=> Status=ProductionStatus.Rejected;
}
