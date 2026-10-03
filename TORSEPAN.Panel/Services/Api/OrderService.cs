using TORSEPAN.Application.Orders;

namespace TORSEPAN.Panel.Services.Api;

public sealed class OrderService(ApiClient api)
{
    public async Task<IReadOnlyList<OrderDto>> GetAsync() => await api.GetAsync<List<OrderDto>>("orders") ?? [];
    public Task<Guid> CreateAsync(CreateOrderRequest request) => api.PostAsync<CreateOrderRequest, Guid>("orders", request);
    public Task AssignCodeAsync(Guid id, string code) => api.PutAsync<AssignOrderCodeRequest, object?>(
        $"orders/{id}/code", new(code));
}
