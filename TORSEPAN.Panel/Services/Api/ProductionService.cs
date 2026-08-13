Exit code: 0
Wall time: 0.6 seconds
Output:
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
    public async Task SellAsync(Guid id,string buyerName,decimal price,string destination) => await _api.PostAsync<object,object?>($"production/{id}/sell",new { BuyerName=buyerName, Price=price, Destination=destination });
    public async Task<IReadOnlyList<SaleItemDto>> GetSalesAsync() => await _api.GetAsync<List<SaleItemDto>>("production/sales") ?? [];
    public Task DeleteAsync(Guid id) => _api.DeleteAsync($"production/{id}");
}

