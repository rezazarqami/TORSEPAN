namespace TORSEPAN.Application.Materials.Queries.GetAllMaterials;

public sealed class MaterialDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Category { get; set; }

    public int Quantity { get; set; }

    public int TopBowlQuantity { get; set; }

    public int BottomBowlQuantity { get; set; }
}
