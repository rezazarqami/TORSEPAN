using TORSEPAN.Panel.Models;
namespace TORSEPAN.Panel.Services.Api;
public sealed class PayrollExclusionService(ApiClient api)
{
    public Task<PayrollExclusionLookupDto?> LookupAsync(string code) => api.GetAsync<PayrollExclusionLookupDto>($"payroll-exclusions/lookup?code={Uri.EscapeDataString(code)}");
    public Task SaveAsync(IEnumerable<Guid> ids, bool excluded) => api.PutAsync<object, object?>("payroll-exclusions", new { EventIds = ids, Excluded = excluded });
}
