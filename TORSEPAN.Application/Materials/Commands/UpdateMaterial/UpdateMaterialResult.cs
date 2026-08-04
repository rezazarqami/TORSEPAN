namespace TORSEPAN.Application.Materials.Commands.UpdateMaterial;

public sealed class UpdateMaterialResult
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;
}