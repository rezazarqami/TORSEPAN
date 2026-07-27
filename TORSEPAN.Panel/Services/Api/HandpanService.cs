using TORSEPAN.Panel.Models;

namespace TORSEPAN.Panel.Services.Api;

public sealed class HandpanService
{
    private readonly ApiClient _api;

    public HandpanService(ApiClient api)
    {
        _api = api;
    }

    public async Task<IReadOnlyList<HandpanDto>> GetReadyForPackagingAsync()
    {
        return await _api.GetAsync<IReadOnlyList<HandpanDto>>(
                   "api/handpans/ready-for-packaging")
               ?? [];
    }

    public async Task<Guid?> CreateAsync(CreateHandpanRequest request)
    {
        return await _api.PostAsync<CreateHandpanRequest, Guid>(
            "api/handpans",
            request);
    }
}