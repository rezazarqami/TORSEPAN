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
        var result = await _api.GetAsync<PagedResult<BowlDto>>("bowls");
        return result?.Items ?? [];
    }

    public async Task<Guid?> CreateAsync(CreateBowlRequest request)
    {
        try
        {
            var response = await _api.PostAsync<CreateBowlRequest, JsonElement>(
                "bowls",
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

    public Task<DimpleBowlDto?> GetForDimplingAsync(string productionCode)
    {
        var code = Uri.EscapeDataString(productionCode.Trim());
        return _api.GetAsync<DimpleBowlDto>($"bowls/dimpling/{code}");
    }

    public Task<DimpleBowlDto?> QueueForDimplingAsync(string productionCode)
    {
        var code = Uri.EscapeDataString(productionCode.Trim());
        return _api.PostAsync<object, DimpleBowlDto>(
            $"bowls/dimpling/{code}/queue",
            new { });
    }
}
