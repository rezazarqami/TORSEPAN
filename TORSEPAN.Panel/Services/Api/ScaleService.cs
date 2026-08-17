using TORSEPAN.Panel.Models;

namespace TORSEPAN.Panel.Services.Api;

public sealed class ScaleService
{
    private readonly ApiClient _api;
    public ScaleService(ApiClient api) => _api = api;

    public async Task<IReadOnlyList<ScaleDto>> GetAsync() =>
        await _api.GetAsync<List<ScaleDto>>("scales") ?? [];

    public Task<Guid> CreateAsync(string name, int usage) =>
        _api.PostAsync<object, Guid>("scales", new { Name = name.Trim(), Usage = usage });
    public Task DeleteAsync(Guid id, int usage) => _api.DeleteAsync($"scales/{id}?usage={usage}");
    public Task RenameAsync(Guid id, string name) =>
        _api.PutAsync<object, object?>($"scales/{id}", new { Name = name.Trim() });
}
