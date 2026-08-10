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

    public Task<int> AddStockAsync(Guid id, int quantity)
    {
        return _api.PatchAsync<object, int>(
            $"materials/{id}/stock",
            new { Quantity = quantity, SetAbsolute = false });
    }

    public Task<int> SetStockAsync(Guid id, int quantity)
    {
        return _api.PatchAsync<object, int>(
            $"materials/{id}/stock",
            new { Quantity = quantity, SetAbsolute = true });
    }

    public async Task AdjustBowlStockAsync(Guid id, int topQuantity, int bottomQuantity, bool setAbsolute)
    {
        await _api.PatchAsync<object, object?>(
            $"materials/{id}/bowl-stock",
            new { TopQuantity = topQuantity, BottomQuantity = bottomQuantity, SetAbsolute = setAbsolute });
    }
    public async Task SetLowStockThresholdAsync(Guid id, int quantity, int top, int bottom)
    {
        await _api.PatchAsync<object, object?>($"materials/{id}/low-stock-threshold",
            new { Quantity = quantity, TopQuantity = top, BottomQuantity = bottom });
    }
}
