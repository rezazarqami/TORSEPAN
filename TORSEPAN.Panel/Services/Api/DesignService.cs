using TORSEPAN.Panel.Models;
namespace TORSEPAN.Panel.Services.Api;
public sealed class DesignService(ApiClient api)
{
    public async Task<List<DesignTypeDto>> GetTypesAsync()=>await api.GetAsync<List<DesignTypeDto>>("designs/types")??[];
    public Task AddTypeAsync(string name)=>api.PostAsync<object,object?>("designs/types",new{Name=name});
    public Task DeleteTypeAsync(Guid id)=>api.DeleteAsync($"designs/types/{id}");
    public Task RegisterAsync(string code,Guid typeId,bool bottom)=>api.PostAsync<object,object?>("designs",new{ProductionCode=code,DesignTypeId=typeId,BottomBowlDesigned=bottom});
}
