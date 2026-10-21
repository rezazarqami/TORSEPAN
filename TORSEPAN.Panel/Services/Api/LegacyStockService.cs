using TORSEPAN.Panel.Models;
namespace TORSEPAN.Panel.Services.Api;
public sealed class LegacyStockService(ApiClient api)
{
    public async Task<List<LegacyStockReceipt>> RecentAsync() => await api.GetAsync<List<LegacyStockReceipt>>("legacy-stock") ?? [];
    public Task<LegacyStockReceipt?> CreateAsync(LegacyStockRequest request) => api.OrderActionAsync<LegacyStockRequest, LegacyStockReceipt>("legacy-stock", request);
}
