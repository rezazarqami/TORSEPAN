namespace TORSEPAN.Application.Materials.Commands.UpdateMaterial;

public sealed class UpdateMaterialResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;
}