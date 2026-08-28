using TORSEPAN.Panel.Models;
namespace TORSEPAN.Panel.Services.Api;
public sealed class HandpanPhotoService(ApiClient api)
{
    public async Task<List<HandpanPhotoDto>> ListAsync(Guid handpanId) => await api.GetAsync<List<HandpanPhotoDto>>($"handpans/{handpanId}/photos") ?? [];
    public async Task<string> DataUrlAsync(Guid handpanId, Guid id, bool thumbnail)
    { var bytes = await api.GetBytesAsync($"handpans/{handpanId}/photos/{id}?thumbnail={thumbnail.ToString().ToLowerInvariant()}"); return $"data:image/webp;base64,{Convert.ToBase64String(bytes)}"; }
    public Task<HandpanPhotoDto?> UploadAsync(Guid handpanId, byte[] image, byte[] thumbnail, string name) => api.PostFileAsync<HandpanPhotoDto>($"handpans/{handpanId}/photos", image, thumbnail, name);
    public Task DeleteAsync(Guid handpanId, Guid id) => api.DeleteAsync($"handpans/{handpanId}/photos/{id}");
}
