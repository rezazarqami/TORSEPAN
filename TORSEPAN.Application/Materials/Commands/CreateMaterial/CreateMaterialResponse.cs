namespace TORSEPAN.Application.Materials.Commands.CreateMaterial;

public sealed class CreateMaterialResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;
}