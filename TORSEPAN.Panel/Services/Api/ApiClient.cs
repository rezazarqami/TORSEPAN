using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TORSEPAN.Application.Auth.Commands.RefreshLogin;
using TORSEPAN.Panel.Models;
using TORSEPAN.Panel.Services.Api;
using TORSEPAN.Panel.Services.Auth;

namespace TORSEPAN.Panel.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly TokenStorage _tokenStorage;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public ApiClient(HttpClient http, TokenStorage tokenStorage)
    {
        _http = http;
        _tokenStorage = tokenStorage;
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
        var response = await SendWithRefreshAsync(() => _http.GetAsync(url));
        return await ReadResponseAsync<T>(response);
    }

    public async Task<TResult?> PostAsync<TRequest, TResult>(
        string url,
        TRequest request)
    {
        var response = await SendWithRefreshAsync(
            () => _http.PostAsJsonAsync(url, request),
            allowRefresh: url != ApiEndpoints.Login && url != ApiEndpoints.Refresh);
        return await ReadResponseAsync<TResult>(response);
    }

    public async Task<TResult?> PutAsync<TRequest, TResult>(
        string url,
        TRequest request)
    {
        var response = await SendWithRefreshAsync(() => _http.PutAsJsonAsync(url, request));
        return await ReadResponseAsync<TResult>(response);
    }

    public async Task DeleteAsync(string url)
    {
        var response = await SendWithRefreshAsync(() => _http.DeleteAsync(url));
        response.EnsureSuccessStatusCode();
    }

    private async Task<HttpResponseMessage> SendWithRefreshAsync(
        Func<Task<HttpResponseMessage>> send,
        bool allowRefresh = true)
    {
        var accessToken = await _tokenStorage.GetAccessTokenAsync();
        SetBearerToken(accessToken);

        var response = await send();
        if (!allowRefresh || response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        if (!await TryRefreshAsync(accessToken))
            return response;

        response.Dispose();
        return await send();
    }

    private async Task<bool> TryRefreshAsync(string? failedAccessToken)
    {
        await _refreshLock.WaitAsync();

        try
        {
            // Another concurrent request may already have refreshed the token.
            var currentAccessToken = await _tokenStorage.GetAccessTokenAsync();
            if (!string.IsNullOrWhiteSpace(currentAccessToken) &&
                currentAccessToken != failedAccessToken)
            {
                SetBearerToken(currentAccessToken);
                return true;
            }

            var refreshToken = await _tokenStorage.GetRefreshTokenAsync();
            if (string.IsNullOrWhiteSpace(refreshToken))
                return false;

            var response = await _http.PostAsJsonAsync(
                ApiEndpoints.Refresh,
                new RefreshTokenRequest { RefreshToken = refreshToken });

            if (!response.IsSuccessStatusCode)
            {
                await _tokenStorage.ClearAsync();
                SetBearerToken(null);
                return false;
            }

            var tokens = await response.Content.ReadFromJsonAsync<RefreshLoginResponse>();
            if (tokens is null ||
                string.IsNullOrWhiteSpace(tokens.AccessToken) ||
                string.IsNullOrWhiteSpace(tokens.RefreshToken))
                return false;

            await _tokenStorage.SaveTokensAsync(
                tokens.AccessToken,
                tokens.RefreshToken);

            SetBearerToken(tokens.AccessToken);
            return true;
        }
        finally
        {
            _refreshLock.Release();
        }
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
