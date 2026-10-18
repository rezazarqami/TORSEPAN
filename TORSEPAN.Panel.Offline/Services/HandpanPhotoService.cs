using Microsoft.JSInterop;
using TORSEPAN.Panel.Models;
namespace TORSEPAN.Panel.Services.Api;
public sealed class HandpanPhotoService(IJSRuntime js){
 public async Task<List<HandpanPhotoDto>> ListAsync(Guid handpanId)=>await js.InvokeAsync<List<HandpanPhotoDto>>("OfflinePanel.photoList",handpanId);
 public async Task<string> DataUrlAsync(Guid handpanId,Guid id,bool thumbnail)=>await js.InvokeAsync<string>("OfflinePanel.photoData",id,thumbnail);
 public Task<HandpanPhotoDto?> UploadAsync(Guid handpanId,byte[] image,byte[] thumbnail,string name)=>throw new InvalidOperationException("عکس را از گالری انتخاب کنید.");
 public async Task DeleteAsync(Guid handpanId,Guid id)=>await js.InvokeVoidAsync("OfflinePanel.deletePhoto",handpanId,id);
}
