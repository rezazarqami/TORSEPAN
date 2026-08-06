using TORSEPAN.Panel.Models;

namespace TORSEPAN.Panel.Services.Api;

public sealed class MaterialService
{
    private readonly ApiClient _api;

    public MaterialService(ApiClient api)
    {
        _api = api;
    }

    public async Task<IReadOnlyList<MaterialDto>> GetAsync()
    {
        return await _api.GetAsync<List<MaterialDto>>("materials")
               ?? [];
    }

    public async Task<Guid?> CreateMaterialAsync(CreateMaterialRequest request)
    {
        return await _api.PostAsync<CreateMaterialRequest, Guid>(
            "materials",
            request);
    }
}
