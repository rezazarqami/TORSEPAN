using TORSEPAN.Panel.Models;

namespace TORSEPAN.Panel.Services.Api;

public sealed class ReportService
{
    private readonly ApiClient _api;

    public ReportService(ApiClient api)
    {
        _api = api;
    }

    public async Task<IReadOnlyList<ReportItemDto>> GetAsync()
    {
        var result =
            await _api.GetAsync<IReadOnlyList<ReportItemDto>>(
                "reports");

        return result ?? [];
    }
}
