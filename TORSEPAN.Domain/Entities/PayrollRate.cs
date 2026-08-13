using TORSEPAN.Domain.Common;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Domain.Entities;

public sealed class PayrollRate : Entity
{
    private PayrollRate() { }
    public PayrollRate(ProductionAction action, Guid? materialId, BowlType? bowlType, Guid? scaleId, decimal amount)
    { Action=action;MaterialId=materialId;BowlType=bowlType;ScaleId=scaleId;SetAmount(amount); }
    public ProductionAction Action { get; private set; }
    public Guid? MaterialId { get; private set; }
    public Material? Material { get; private set; }
    public BowlType? BowlType { get; private set; }
    public Guid? ScaleId { get; private set; }
    public Scale? Scale { get; private set; }
    public decimal Amount { get; private set; }
    public void SetAmount(decimal amount){if(amount<0)throw new ArgumentOutOfRangeException(nameof(amount));Amount=amount;}
}
