namespace TORSEPAN.Application.Materials.Commands.CreateMaterial;

public sealed class CreateMaterialResult
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;
}