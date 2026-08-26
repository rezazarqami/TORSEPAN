using TORSEPAN.Panel.Models;
namespace TORSEPAN.Panel.Services.Api;
public sealed class DesignService(ApiClient api)
{
    public async Task<List<DesignTypeDto>> GetTypesAsync()=>await api.GetAsync<List<DesignTypeDto>>("designs/types")??[];
    public async Task<List<DesignUserDto>> GetUsersAsync()=>await api.GetAsync<List<DesignUserDto>>("designs/users")??[];
    public Task AddTypeAsync(string name,decimal rate)=>api.PostAsync<object,object?>("designs/types",new{Name=name,Rate=rate});
    public Task UpdateTypeAsync(Guid id,string name,decimal rate)=>api.PutAsync<object,object?>($"designs/types/{id}",new{Name=name,Rate=rate});
    public Task DeleteTypeAsync(Guid id)=>api.DeleteAsync($"designs/types/{id}");
    public Task RegisterAsync(string code,IEnumerable<(Guid TypeId,Guid? UserId)> items,bool bottom)=>api.PostAsync<object,object?>("designs",new{ProductionCode=code,Items=items.Select(x=>new{DesignTypeId=x.TypeId,x.UserId}),BottomBowlDesigned=bottom});
}
