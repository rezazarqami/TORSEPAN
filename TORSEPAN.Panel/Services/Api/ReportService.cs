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

    public async Task<ProductionAnalyticsDto> GetProductionAsync(DateTime? from,DateTime? to,IReadOnlyCollection<Guid> materials,IReadOnlyCollection<Guid> scales,string payroll,string destination)
        => await _api.GetAsync<ProductionAnalyticsDto>("management-reports/production"+Query(from,to,materials,scales,payroll,destination))??new();

    public async Task<InventoryReportDto> GetInventoryAsync(DateTime? from,DateTime? to,string kind,IReadOnlyCollection<Guid> materials,IReadOnlyCollection<Guid> scales,string destination)
        => await _api.GetAsync<InventoryReportDto>("management-reports/inventory"+Query(from,to,materials,scales,"all",destination,$"kind={Uri.EscapeDataString(kind)}"))??new();

    public Task<byte[]> PdfAsync(string kind,DateTime? from,DateTime? to,IReadOnlyCollection<Guid> materials,IReadOnlyCollection<Guid> scales,string payroll,string destination,string inventoryKind,Guid? userId,int? action)
    {
        var extra=$"inventoryKind={Uri.EscapeDataString(inventoryKind)}"+(userId.HasValue?$"&userId={userId}":"")+(action.HasValue?$"&action={action}":"");
        return _api.GetBytesAsync($"management-reports/{kind}/pdf"+Query(from,to,materials,scales,payroll,destination,extra));
    }

    public Task SendTelegramAsync(string kind,DateTime? from,DateTime? to,IReadOnlyCollection<Guid> materials,IReadOnlyCollection<Guid> scales,string payroll,string destination,string inventoryKind,Guid? userId,int? action)
        => _api.PostAsync<object,object?>($"management-reports/{kind}/telegram",new{From=from,To=to,MaterialIds=materials,ScaleIds=scales,Payroll=payroll,Destination=destination,InventoryKind=inventoryKind,UserId=userId,Action=action});

    private static string Query(DateTime? from,DateTime? to,IReadOnlyCollection<Guid> materials,IReadOnlyCollection<Guid> scales,string payroll,string destination,string? extra=null)
    {var v=new List<string>();if(from.HasValue)v.Add($"from={from:yyyy-MM-dd}");if(to.HasValue)v.Add($"to={to:yyyy-MM-dd}");if(materials.Count>0)v.Add($"materialIds={string.Join(',',materials)}");if(scales.Count>0)v.Add($"scaleIds={string.Join(',',scales)}");v.Add($"payroll={Uri.EscapeDataString(payroll)}");v.Add($"destination={Uri.EscapeDataString(destination)}");if(!string.IsNullOrWhiteSpace(extra))v.Add(extra);return"?"+string.Join('&',v);}
}
