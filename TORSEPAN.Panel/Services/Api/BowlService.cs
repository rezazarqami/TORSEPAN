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

    public Task DeleteAsync(Guid id) => _api.DeleteAsync($"bowls/{id}");

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
            throw new Exception("کد تولید تکراری است یا موجودی کاسه انتخاب‌شده کافی نیست.");
        }
    }

    public Task<DimpleBowlDto?> GetForDimplingAsync(string productionCode)
    {
        var code = Uri.EscapeDataString(productionCode.Trim());
        return _api.GetAsync<DimpleBowlDto>($"bowls/dimpling/{code}");
    }

    public Task<DimpleBowlDto?> CompleteDimplingAsync(
        string productionCode,
        int duration)
    {
        var code = Uri.EscapeDataString(productionCode.Trim());
        return _api.PostAsync<object, DimpleBowlDto>(
            $"bowls/dimpling/{code}/complete",
            new { Duration = duration });
    }

    public Task<DimpleBowlDto?> CompleteShapeAsync(
        string productionCode,
        int duration)
    {
        var code = Uri.EscapeDataString(productionCode.Trim());
        return _api.PostAsync<object, DimpleBowlDto>(
            $"bowls/production/{code}/shape/complete",
            new { Duration = duration });
    }

    public Task<DimpleBowlDto?> CompleteBakeAsync(string productionCode)
    {
        var code = Uri.EscapeDataString(productionCode.Trim());
        return _api.PostAsync<object, DimpleBowlDto>(
            $"bowls/production/{code}/bake/complete",
            new { });
    }

    public Task<DimpleBowlDto?> CompleteTuneAsync(
        string productionCode,
        int duration)
    {
        var code = Uri.EscapeDataString(productionCode.Trim());
        return _api.PostAsync<object, DimpleBowlDto>(
            $"bowls/production/{code}/tune/complete",
            new { Duration = duration });
    }

    public Task<DimpleBowlDto?> CompleteGlueAsync(
        string productionCode,
        string pairedProductionCode,
        Guid scaleId)
    {
        var code = Uri.EscapeDataString(productionCode.Trim());
        return _api.PostAsync<object, DimpleBowlDto>(
            $"bowls/production/{code}/glue/complete",
            new { PairedProductionCode = pairedProductionCode.Trim(), ScaleId = scaleId });
    }

    public Task<DimpleBowlDto?> ReleaseFromGlueRoomAsync(string productionCode)
    {
        var code = Uri.EscapeDataString(productionCode.Trim());
        return _api.PostAsync<object, DimpleBowlDto>(
            $"bowls/production/{code}/glue-room/release",
            new { });
    }

    public Task<DimpleBowlDto?> CompleteFinalTuneAsync(string productionCode, int duration)
    {
        var code = Uri.EscapeDataString(productionCode.Trim());
        return _api.PostAsync<object, DimpleBowlDto>(
            $"bowls/production/{code}/final-tune/complete",
            new { Duration = duration });
    }

    public Task<DimpleBowlDto?> CompleteQualityControlAsync(
        string productionCode,
        bool approved,
        string? rejectionReason = null,
        string? details = null)
    {
        var code = Uri.EscapeDataString(productionCode.Trim());
        return _api.PostAsync<object, DimpleBowlDto>(
            $"bowls/production/{code}/qc/complete",
            new { Approved = approved, RejectionReason = rejectionReason, Details = details });
    }

    public Task<DimpleBowlDto?> CompletePackagingAsync(string productionCode, IReadOnlyCollection<Guid> materialIds)
    {
        var code = Uri.EscapeDataString(productionCode.Trim());
        return _api.PostAsync<object, DimpleBowlDto>(
            $"bowls/production/{code}/packaging/complete",
            new { MaterialIds = materialIds });
    }
}
