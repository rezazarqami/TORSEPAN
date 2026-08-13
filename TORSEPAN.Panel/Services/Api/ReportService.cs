using TORSEPAN.Panel.Models;

namespace TORSEPAN.Panel.Services.Api;

public sealed class ReportService
{
    private readonly ApiClient _api;

    public ReportService(ApiClient api)
    {
        _api = api;
    }

    public async Task<ProductionReportDto> GetAsync(DateTime? from = null, DateTime? to = null, Guid? userId = null, int? action = null, int? result = null)
    {
        var values = new List<string>();
        if (from.HasValue) values.Add($"from={from:yyyy-MM-dd}");
        if (to.HasValue) values.Add($"to={to:yyyy-MM-dd}");
        if (userId.HasValue) values.Add($"userId={userId}");
        if (action.HasValue) values.Add($"action={action}");
        if (result.HasValue) values.Add($"result={result}");
        var url = "production/report" + (values.Count > 0 ? "?" + string.Join("&", values) : "");
        return await _api.GetAsync<ProductionReportDto>(url) ?? new ProductionReportDto();
    }
}
