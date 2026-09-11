namespace TORSEPAN.Panel.Models;

public sealed class SaleOperationResult
{
    public bool SaleRegistered { get; set; }
    public bool WarrantyActivated { get; set; }
    public string? Warning { get; set; }
}
