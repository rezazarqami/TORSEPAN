using TORSEPAN.Panel.Models;

namespace TORSEPAN.Panel.Services.Api;

public sealed class HandpanService
{
    private readonly ApiClient _api;

    public HandpanService(ApiClient api)
    {
        _api = api;
    }

    public async Task<Guid?> CreateAsync(CreateHandpanRequest request)
    {
        return await _api.PostAsync<CreateHandpanRequest, Guid?>(
            "handpans",
            request);
    }

    public async Task<HandpanDto?> GetAsync(Guid id)
    {
        return await _api.GetAsync<HandpanDto>(
            $"handpans/{id}");
    }

    public async Task<IReadOnlyList<HandpanDto>> GetReadyForPackagingAsync()
    {
        return await _api.GetAsync<IReadOnlyList<HandpanDto>>(
                   "handpans/ready-for-packaging")
               ?? [];
    }

    public async Task<IReadOnlyList<HandpanDto>> GetAllAsync()
    {
        return await _api.GetAsync<IReadOnlyList<HandpanDto>>("handpans")
               ?? [];
    }

    public Task DeleteAsync(Guid id) => _api.DeleteAsync($"production/{id}");
}
