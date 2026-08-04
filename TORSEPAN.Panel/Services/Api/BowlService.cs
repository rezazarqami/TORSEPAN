using System.Net;
using System.Text.Json;
using TORSEPAN.Panel.Models;

namespace TORSEPAN.Panel.Services.Api;

public sealed class BowlService
{
    private readonly ApiClient _api;

    public BowlService(ApiClient api)
    {
        _api = api;
    }

    public async Task<IReadOnlyList<BowlDto>> GetAsync()
    {
        var result = await _api.GetAsync<PagedResult<BowlDto>>("api/bowls");
        return result?.Items ?? [];
    }

    public async Task<Guid?> CreateAsync(CreateBowlRequest request)
    {
        try
        {
            var response = await _api.PostAsync<CreateBowlRequest, JsonElement>(
                "api/bowls",
                request);

            if (response.ValueKind == JsonValueKind.Object &&
                response.TryGetProperty("id", out var idElement) &&
                idElement.ValueKind == JsonValueKind.String &&
                Guid.TryParse(idElement.GetString(), out var id))
            {
                return id;
            }

            return null;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new Exception("کد تولید قبلاً ثبت شده است.");
        }
    }
}