using System.Net.Http.Json;

namespace TORSEPAN.API.Services;

public sealed class GuaranteeServiceClient(HttpClient httpClient, IConfiguration configuration)
{
    public async Task<IReadOnlyDictionary<string, WarrantyStatusDto>> GetStatusesAsync(
        IEnumerable<string> productCodes,
        CancellationToken cancellationToken = default)
    {
        var codes = productCodes.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToUpperInvariant()).Distinct().ToArray();
        if (codes.Length == 0) return new Dictionary<string, WarrantyStatusDto>();

        using var request = CreateRequest(HttpMethod.Post, "api/warranties/internal/status");
        request.Content = JsonContent.Create(new { ProductCodes = codes });
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<WarrantyStatusDto>>(cancellationToken: cancellationToken) ?? [];
        return items.ToDictionary(x => x.ProductCode, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<WarrantyActivationResult> ActivateAsync(
        string productCode,
        string fullName,
        string phoneNumber,
        string city,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Post, "api/warranties/internal/activate");
            request.Content = JsonContent.Create(new { ProductCode = productCode, FullName = fullName, PhoneNumber = phoneNumber, City = city });
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode) return new WarrantyActivationResult(true, null);
            return new WarrantyActivationResult(false, "فروش ثبت شد، اما فعال‌سازی گارانتی انجام نشد.");
        }
        catch
        {
            return new WarrantyActivationResult(false, "فروش ثبت شد، اما سرویس گارانتی در دسترس نبود.");
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        var apiKey = configuration["GuaranteeIntegration:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey)) request.Headers.Add("X-Guarantee-Key", apiKey);
        return request;
    }
}

public sealed record WarrantyStatusDto(string ProductCode, bool IsActive, DateTime? ActivatedAtUtc);
public sealed record WarrantyActivationResult(bool IsActive, string? Error);
