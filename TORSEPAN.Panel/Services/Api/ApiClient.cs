using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TORSEPAN.Panel.Services;

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public void SetBearerToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _http.DefaultRequestHeaders.Authorization = null;
            return;
        }

        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        return await _http.GetFromJsonAsync<T>(url);
    }

    public async Task<TResult?> PostAsync<TRequest, TResult>(
        string url,
        TRequest request)
    {
        var response = await _http.PostAsJsonAsync(url, request);
        return await ReadResponseAsync<TResult>(response);
    }

    public async Task<TResult?> PutAsync<TRequest, TResult>(
        string url,
        TRequest request)
    {
        var response = await _http.PutAsJsonAsync(url, request);
        return await ReadResponseAsync<TResult>(response);
    }

    public async Task DeleteAsync(string url)
    {
        var response = await _http.DeleteAsync(url);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<TResult?> ReadResponseAsync<TResult>(
        HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();

        if (response.StatusCode == HttpStatusCode.NoContent ||
            response.Content.Headers.ContentLength == 0)
            return default;

        var content = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(content))
            return default;

        return JsonSerializer.Deserialize<TResult>(
            content,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
