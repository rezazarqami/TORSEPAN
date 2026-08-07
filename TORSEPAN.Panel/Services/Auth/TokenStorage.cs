using Microsoft.JSInterop;

namespace TORSEPAN.Panel.Services.Auth;

public sealed class TokenStorage(IJSRuntime js)
{
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";

    public async Task<string?> GetAccessTokenAsync()
    {
        return await js.InvokeAsync<string?>(
            "localStorage.getItem",
            AccessTokenKey);
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        return await js.InvokeAsync<string?>(
            "localStorage.getItem",
            RefreshTokenKey);
    }

    public async Task SaveTokensAsync(string accessToken, string refreshToken)
    {
        await js.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, accessToken);
        await js.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, refreshToken);
    }

    public async Task ClearAsync()
    {
        await js.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
        await js.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);

        // Remove tokens left by older deployments as well.
        await js.InvokeVoidAsync("sessionStorage.removeItem", AccessTokenKey);
        await js.InvokeVoidAsync("sessionStorage.removeItem", RefreshTokenKey);
    }
}
