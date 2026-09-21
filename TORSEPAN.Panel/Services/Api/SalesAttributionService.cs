using TORSEPAN.Panel.Models;
namespace TORSEPAN.Panel.Services.Api;
public sealed class SalesAttributionService(ApiClient api)
{
    public async Task<SalesAttributionOptionsDto> OptionsAsync()=>await api.GetAsync<SalesAttributionOptionsDto>("sales-attribution/options")??new();
    public Task<SalesAttributionOptionDto?> AddSourceAsync(string name)=>api.PostAsync<object,SalesAttributionOptionDto>("sales-attribution/sources",new{Name=name});
    public Task<SalesAttributionOptionDto?> AddReferrerAsync(string name)=>api.PostAsync<object,SalesAttributionOptionDto>("sales-attribution/referrers",new{Name=name});
    public async Task<SalesAttributionReportDto> ReportAsync(DateTime? from,DateTime? to)=>await api.GetAsync<SalesAttributionReportDto>($"sales-attribution/report?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}")??new();
}
