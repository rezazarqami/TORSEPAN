using Microsoft.JSInterop;

namespace TORSEPAN.Panel.Services.Auth;

public sealed class TokenStorage(IJSRuntime js)
{
    private const string AccessTokenKey = "access_token";

    public async Task<string?> GetAccessTokenAsync()
    {
        return await js.InvokeAsync<string?>(
            "sessionStorage.getItem",
            AccessTokenKey);
    }

    public async Task SaveAccessTokenAsync(string token)
    {
        await js.InvokeVoidAsync(
            "sessionStorage.setItem",
            AccessTokenKey,
            token);
    }

    public async Task ClearAsync()
    {
        await js.InvokeVoidAsync(
            "sessionStorage.removeItem",
            AccessTokenKey);
    }
}
