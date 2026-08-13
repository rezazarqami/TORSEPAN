Exit code: 0
Wall time: 0.7 seconds
Output:
namespace TORSEPAN.Panel.Models;

public sealed class MaterialDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Category { get; set; }

    public int Quantity { get; set; }

    public int TopBowlQuantity { get; set; }

    public int BottomBowlQuantity { get; set; }
    public int LowStockThreshold { get; set; }
    public int TopBowlLowStockThreshold { get; set; }
    public int BottomBowlLowStockThreshold { get; set; }
    public string TopBowlCodeTemplate { get; set; } = string.Empty;
    public string BottomBowlCodeTemplate { get; set; } = string.Empty;
}

