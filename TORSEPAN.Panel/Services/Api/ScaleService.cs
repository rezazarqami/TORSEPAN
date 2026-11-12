using SaveVirtualScale = TORSEPAN.Application.Scales.SaveVirtualScale;
using VirtualScaleSummary = TORSEPAN.Application.Scales.VirtualScaleSummary;
using TORSEPAN.Panel.Models;

namespace TORSEPAN.Panel.Services.Api;

public sealed class ScaleService
{
    private readonly ApiClient _api;
    public ScaleService(ApiClient api) => _api = api;

    public async Task<IReadOnlyList<ScaleDto>> GetAsync(bool forceRefresh = false) =>
        await _api.GetAsync<List<ScaleDto>>("scales", forceRefresh) ?? [];

    public async Task<IReadOnlyList<LegacyTopBowlScaleDto>> GetLegacyTopBowlsAsync() =>
        await _api.GetAsync<List<LegacyTopBowlScaleDto>>("scales/legacy-top-bowls") ?? [];

    public Task<ScalePhotoSources?> GetPhotosAsync(Guid id) =>
        _api.GetAsync<ScalePhotoSources>($"scales/{id}/photos");

    public Task<ScalePhotoSources?> GetPlaygroundAsync(Guid id) =>
        _api.GetAsync<ScalePhotoSources>($"scales/{id}/playground");

    public async Task<IReadOnlyList<VirtualScaleSummary>> GetVirtualDesignsAsync() =>
        await _api.GetAsync<List<VirtualScaleSummary>>("scales/virtual-designs") ?? [];

    public Task<ScalePhotoSources?> GetVirtualDesignAsync(Guid id) =>
        _api.GetAsync<ScalePhotoSources>($"scales/virtual-designs/{id}");

    public Task SaveVirtualDesignAsync(Guid id, SaveVirtualScale request) =>
        _api.PutAsync<SaveVirtualScale, object?>($"scales/virtual-designs/{id}", request);

    public Task<Guid> CreateAsync(string name, int usage, byte[]? topPhoto = null, byte[]? bottomPhoto = null) =>
        _api.PostAsync<object, Guid>("scales", topPhoto is null && bottomPhoto is null
            ? new { Name = name.Trim(), Usage = usage }
            : new { Name = name.Trim(), Usage = usage, TopPhoto = topPhoto, BottomPhoto = bottomPhoto });
    public Task DeleteAsync(Guid id, int usage) => _api.DeleteAsync($"scales/{id}?usage={usage}");
    public Task RenameAsync(Guid id, string name, byte[]? topPhoto = null, byte[]? bottomPhoto = null) =>
        _api.PutAsync<object, object?>($"scales/{id}", topPhoto is null && bottomPhoto is null
            ? new { Name = name.Trim() }
            : new { Name = name.Trim(), TopPhoto = topPhoto, BottomPhoto = bottomPhoto });
}
