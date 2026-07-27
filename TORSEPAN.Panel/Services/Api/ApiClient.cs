using System.Net.Http.Json;

namespace TORSEPAN.Panel.Services;

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
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

        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength == 0)
            return default;

        return await response.Content.ReadFromJsonAsync<TResult>();
    }

    public async Task<TResult?> PutAsync<TRequest, TResult>(
        string url,
        TRequest request)
    {
        var response = await _http.PutAsJsonAsync(url, request);

        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength == 0)
            return default;

        return await response.Content.ReadFromJsonAsync<TResult>();
    }

    public async Task DeleteAsync(string url)
    {
        var response = await _http.DeleteAsync(url);

        response.EnsureSuccessStatusCode();
    }
}