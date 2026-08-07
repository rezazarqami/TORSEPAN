using TORSEPAN.Panel.Models;

namespace TORSEPAN.Panel.Services.Api;

public sealed class ScaleService
{
    private readonly ApiClient _api;
    public ScaleService(ApiClient api) => _api = api;

    public async Task<IReadOnlyList<ScaleDto>> GetAsync() =>
        await _api.GetAsync<List<ScaleDto>>("scales") ?? [];

    public Task<Guid> CreateAsync(string name) =>
        _api.PostAsync<object, Guid>("scales", new { Name = name.Trim() });
}
