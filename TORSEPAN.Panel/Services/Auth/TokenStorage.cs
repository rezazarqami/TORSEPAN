using Microsoft.JSInterop;

namespace TORSEPAN.Panel.Services.Auth;

public sealed class TokenStorage(IJSRuntime js)
{
    private const string AccessTokenKey = "access_token";

    public async Task<string?> GetAccessTokenAsync()
    {
        return await js.InvokeAsync<string?>(
            "localStorage.getItem",
            AccessTokenKey);
    }

    public async Task SaveAccessTokenAsync(string token)
    {
        await js.InvokeVoidAsync(
            "localStorage.setItem",
            AccessTokenKey,
            token);
    }

    public async Task ClearAsync()
    {
        await js.InvokeVoidAsync(
            "localStorage.removeItem",
            AccessTokenKey);
    }
}