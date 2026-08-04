namespace TORSEPAN.Application.Materials.Queries.GetAllMaterials;

public sealed class GetAllMaterialsResponse
{
    public IReadOnlyList<MaterialDto> Materials { get; init; }
        = Array.Empty<MaterialDto>();
}