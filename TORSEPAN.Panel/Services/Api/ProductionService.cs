using TORSEPAN.Panel.Models;

namespace TORSEPAN.Panel.Services.Api;

public sealed class ProductionService
{
    private readonly ApiClient _api;

    public ProductionService(ApiClient api)
    {
        _api = api;
    }

    public async Task<ProductionDashboardDto?> GetDashboardAsync()
    {
        return await _api.GetAsync<ProductionDashboardDto>(
            "production/dashboard");
    }

    public async Task<IReadOnlyList<ProductionStageItemDto>> GetQueueAsync()
    {
        var result =
            await _api.GetAsync<IReadOnlyList<ProductionStageItemDto>>(
                "production/queue");

        return result ?? [];
    }

    public async Task<bool> ChangeStageAsync(ChangeProductionStageRequest request)
    {
        return await _api.PostAsync<ChangeProductionStageRequest, bool>(
            "production/change-stage",
            request);
    }

    public async Task<IReadOnlyList<WarehouseHandpanDto>> GetWarehouseAsync()
    {
        return await _api.GetAsync<List<WarehouseHandpanDto>>(
            "production/warehouse") ?? [];
    }
}
