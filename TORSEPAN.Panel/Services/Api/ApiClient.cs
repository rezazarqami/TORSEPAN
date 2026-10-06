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

    public async Task<bool> RenewSessionAsync()
    {
        var current = await _tokenStorage.GetAccessTokenAsync();
        return await TryRefreshAsync(current, force: true);
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        var response = await SendWithRefreshAsync(() => _http.GetAsync(url));
        return await ReadResponseAsync<T>(response);
    }

    public async Task<TResult?> PostAsync<TRequest, TResult>(
        string url,
        TRequest request,
        CancellationToken cancellationToken = default,
        bool reportErrorDetail = false)
    {
        var response = await SendWithRefreshAsync(
            () => _http.PostAsJsonAsync(url, request, cancellationToken),
            allowRefresh: url != ApiEndpoints.Login && url != ApiEndpoints.Refresh);
        if (reportErrorDetail && !response.IsSuccessStatusCode)
        {
            var detail = $"API با کد {(int)response.StatusCode} پاسخ داد.";
            try
            {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                if (json.RootElement.TryGetProperty("detail", out var value) && value.ValueKind == JsonValueKind.String)
                {
                    var message = value.GetString();
                    if (!string.IsNullOrWhiteSpace(message)) detail = message[..Math.Min(message.Length, 240)];
                }
            }
            catch (JsonException) { }
            var status = response.StatusCode;
            response.Dispose();
            throw new HttpRequestException(detail, null, status);
        }
        return await ReadResponseAsync<TResult>(response);
    }

    public async Task<TResult?> PutAsync<TRequest, TResult>(
        string url,
        TRequest request)
    {
        var response = await SendWithRefreshAsync(() => _http.PutAsJsonAsync(url, request));
        return await ReadResponseAsync<TResult>(response);
    }

    public async Task<TResult?> PutAccountAsync<TRequest, TResult>(string url, TRequest request)
    {
        using var response = await SendWithRefreshAsync(() => _http.PutAsJsonAsync(url, request));
        if (!response.IsSuccessStatusCode)
        {
            string? message = null;
            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
            {
                try { message = (await response.Content.ReadFromJsonAsync<AccountError>())?.Message; } catch (JsonException) { }
            }
            message ??= response.StatusCode == HttpStatusCode.TooManyRequests ? "تعداد تلاش‌ها زیاد است؛ یک دقیقه بعد دوباره امتحان کنید." : "ذخیره انجام نشد؛ اتصال و ورود به حساب را بررسی کنید.";
            throw new AccountActionException(message);
        }
        return await ReadResponseAsync<TResult>(response);
    }
    private sealed class AccountError { public string? Message {get;set;} }

    public async Task<TResult?> PatchAsync<TRequest, TResult>(string url, TRequest request)
    {
        var response = await SendWithRefreshAsync(() => _http.PatchAsJsonAsync(url, request));
        return await ReadResponseAsync<TResult>(response);
    }

    public async Task DeleteAccountAsync(string url)
    {
        using var response = await SendWithRefreshAsync(() => _http.DeleteAsync(url));
        if (response.IsSuccessStatusCode) return;
        string? message = null;
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict or HttpStatusCode.NotFound)
        { try { message = (await response.Content.ReadFromJsonAsync<AccountError>())?.Message; } catch (JsonException) { } }
        message ??= response.StatusCode switch
        {
            HttpStatusCode.Forbidden => "حذف کاربر فقط برای ادمین و مدیر تولید مجاز است. نقش فعلی حساب را بررسی کنید.",
            HttpStatusCode.Unauthorized => "نشست ورود معتبر نیست؛ دوباره وارد حساب شوید.",
            HttpStatusCode.MethodNotAllowed => "نسخه API هنوز از حذف حساب پشتیبانی نمی‌کند؛ نسخه سرور باید به‌روز شود.",
            _ => $"حذف کاربر انجام نشد (خطای {(int)response.StatusCode}). دوباره تلاش کنید."
        };
        throw new AccountActionException(message);
    }

    public async Task DeleteAsync(string url)
    {
        var response = await SendWithRefreshAsync(() => _http.DeleteAsync(url));
        response.EnsureSuccessStatusCode();
    }

    public async Task<byte[]> GetBytesAsync(string url)
    {
        var response = await SendWithRefreshAsync(() => _http.GetAsync(url));
        response.EnsureSuccessStatusCode(); return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<T?> PostFileAsync<T>(string url, byte[] image, byte[] thumbnail, string fileName)
    {
        async Task<HttpResponseMessage> Send()
        {
            var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(image); file.Headers.ContentType = new MediaTypeHeaderValue("image/webp");
            var thumb = new ByteArrayContent(thumbnail); thumb.Headers.ContentType = new MediaTypeHeaderValue("image/webp");
            form.Add(file, "file", fileName + ".webp"); form.Add(thumb, "thumbnail", fileName + "-thumb.webp"); return await _http.PostAsync(url, form);
        }
        var response = await SendWithRefreshAsync(Send);
        return await ReadResponseAsync<T>(response);
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

    private async Task<bool> TryRefreshAsync(string? failedAccessToken, bool force = false)
    {
        await _refreshLock.WaitAsync();

        try
        {
            // Another concurrent request may already have refreshed the token.
            var currentAccessToken = await _tokenStorage.GetAccessTokenAsync();
            if (!force && !string.IsNullOrWhiteSpace(currentAccessToken) &&
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
