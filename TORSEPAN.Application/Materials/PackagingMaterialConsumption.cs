namespace TORSEPAN.Application.Materials;

public static class PackagingMaterialConsumption
{
    public static int RequiredQuantity(string name)
        => name.Trim().Replace('ي', 'ی') == "مثلثی" ? 2 : 1;
}
