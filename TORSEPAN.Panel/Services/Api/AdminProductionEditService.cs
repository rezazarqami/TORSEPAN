using TORSEPAN.Panel.Models;

namespace TORSEPAN.Panel.Services.Api;

public sealed class AdminProductionEditService(ApiClient api)
{
    public Task<AdminProductionEditDto?> GetAsync(string code)=>
        api.GetAsync<AdminProductionEditDto>($"admin-production-edits/{Uri.EscapeDataString(code.Trim())}");

    public Task<AdminProductionEditDto?> UpdateAsync(string code,AdminProductionEditRequest request)=>
        api.PutAsync<AdminProductionEditRequest,AdminProductionEditDto>($"admin-production-edits/{Uri.EscapeDataString(code.Trim())}",request);
}
