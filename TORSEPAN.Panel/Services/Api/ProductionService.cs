using TORSEPAN.Panel.Models;

namespace TORSEPAN.Panel.Services.Api;

public sealed class ProductionService
{
    private readonly ApiClient _api;

    public ProductionService(ApiClient api)
    {
        _api = api;
    }

    public async Task<ProductionDashboardDto> GetDashboardAsync()
    {
        return await _api.GetAsync<ProductionDashboardDto>(
                   "api/production/dashboard")
               ?? new();
    }
}