Exit code: 0
Wall time: 0.7 seconds
Output:
namespace TORSEPAN.Domain.Entities;

public sealed class Material
{
    private Material()
    {
    }

    public Material(string name, MaterialCategory category = MaterialCategory.Other, int quantity = 0)
    {
        Id = Guid.NewGuid();
        Name = name;
        Category = category;
        Quantity = quantity < 0 ? 0 : quantity;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public MaterialCategory Category { get; private set; }

    public int Quantity { get; private set; }

    public int TopBowlQuantity { get; private set; }

    public int BottomBowlQuantity { get; private set; }
    public int LowStockThreshold { get; private set; }
    public int TopBowlLowStockThreshold { get; private set; }
    public int BottomBowlLowStockThreshold { get; private set; }
    public string TopBowlCodeTemplate { get; private set; } = string.Empty;
    public string BottomBowlCodeTemplate { get; private set; } = string.Empty;

    public void SetBowlCodeTemplates(string? topTemplate, string? bottomTemplate)
    {
        TopBowlCodeTemplate = topTemplate?.Trim().ToUpperInvariant() ?? string.Empty;
        BottomBowlCodeTemplate = bottomTemplate?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    public void SetLowStockThresholds(int quantity, int top, int bottom)
    {
        if (quantity < 0 || top < 0 || bottom < 0) throw new ArgumentOutOfRangeException();
        LowStockThreshold = quantity; TopBowlLowStockThreshold = top; BottomBowlLowStockThreshold = bottom;
    }

    public void Rename(string name)
    {
        Name = name;
    }

    public void SetCategory(MaterialCategory category) => Category = category;

    public void AddStock(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        Quantity += amount;
    }

    public void SetStock(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        Quantity = quantity;
    }

    public bool TryConsume(int amount = 1)
    {
        if (amount <= 0 || Quantity < amount)
            return false;

        Quantity -= amount;
        return true;
    }

    public void AddBowlStock(int topQuantity, int bottomQuantity)
    {
        if (topQuantity < 0 || bottomQuantity < 0 || (topQuantity == 0 && bottomQuantity == 0))
            throw new ArgumentOutOfRangeException(nameof(topQuantity));

        TopBowlQuantity += topQuantity;
        BottomBowlQuantity += bottomQuantity;
    }

    public void SetBowlStock(int topQuantity, int bottomQuantity)
    {
        if (topQuantity < 0 || bottomQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(topQuantity));

        TopBowlQuantity = topQuantity;
        BottomBowlQuantity = bottomQuantity;
    }

    public bool TryConsumeBowl(bool isTop)
    {
        if (isTop)
        {
            if (TopBowlQuantity < 1) return false;
            TopBowlQuantity--;
            return true;
        }

        if (BottomBowlQuantity < 1) return false;
        BottomBowlQuantity--;
        return true;
    }
}

public enum MaterialCategory
{
    TopBowl = 1,
    BottomBowl = 2,
    Other = 3,
    BowlMaterial = 4
}

