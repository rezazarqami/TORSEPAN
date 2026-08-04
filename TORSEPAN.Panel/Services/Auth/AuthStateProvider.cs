using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using TORSEPAN.Panel.Authentication;

namespace TORSEPAN.Panel.Services.Auth;

public sealed class AuthStateProvider(TokenStorage storage)
    : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // هنگام Prerender به localStorage دسترسی نزن.
        return Task.FromResult(Anonymous);
    }

    public async Task RefreshAsync()
    {
        try
        {
            var token = await storage.GetAccessTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
                return;
            }

            var identity = new ClaimsIdentity(
                JwtParser.ParseClaims(token),
                "jwt");

            NotifyAuthenticationStateChanged(
                Task.FromResult(
                    new AuthenticationState(
                        new ClaimsPrincipal(identity))));
        }
        catch
        {
            NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
        }
    }

    public void NotifyUserLogout()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }
}