using TORSEPAN.Application.Orders;

namespace TORSEPAN.Panel.Services.Api;

public sealed class OrderService(ApiClient api)
{
    public Task<Guid> SaveDraftAsync(Guid? id, SaveOrderDraftRequest request) => api.OrderActionAsync<SaveOrderDraftRequest, Guid>(
        id.HasValue ? $"orders/{id}/draft" : "orders/drafts", request, id.HasValue);
    public Task UpdateAsync(Guid id, SaveOrderDraftRequest request) =>
        api.OrderActionAsync<SaveOrderDraftRequest,object?>($"orders/{id}",request,true);
    public Task FinalizeAsync(Guid id, int version) => api.OrderActionAsync<FinalizeOrderRequest, object?>($"orders/{id}/finalize", new(version));
    public Task AssignLineCodeAsync(Guid id, Guid lineId, int slot, string code, string expectedCode) => api.OrderActionAsync<AssignOrderCodeRequest, object?>(
        $"orders/{id}/lines/{lineId}/codes/{slot}", new(code, expectedCode), true);
    public Task DeleteAsync(Guid id) => api.DeleteAsync($"orders/{id}");
    public async Task<IReadOnlyList<OrderDto>> GetAsync() => await api.GetAsync<List<OrderDto>>("orders") ?? [];
    public Task<Guid> CreateAsync(CreateOrderRequest request) => api.PostAsync<CreateOrderRequest, Guid>("orders", request);
    public Task AssignCodeAsync(Guid id, string code) => api.PutAsync<AssignOrderCodeRequest, object?>(
        $"orders/{id}/code", new(code));
}
