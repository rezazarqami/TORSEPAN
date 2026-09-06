using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using TORSEPAN.Panel.Authentication;

namespace TORSEPAN.Panel.Services.Auth;

public sealed class AuthStateProvider(TokenStorage storage)
    : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));
    private AuthenticationState _current = Anonymous;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // هنگام Prerender به localStorage دسترسی نزن.
        return Task.FromResult(_current);
    }

    public async Task RefreshAsync()
    {
        try
        {
            var token = await storage.GetAccessTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                _current = Anonymous;
                NotifyAuthenticationStateChanged(Task.FromResult(_current));
                return;
            }

            var identity = new ClaimsIdentity(
                JwtParser.ParseClaims(token),
                "jwt");

            _current = new AuthenticationState(new ClaimsPrincipal(identity));
            NotifyAuthenticationStateChanged(Task.FromResult(_current));
        }
        catch
        {
            _current = Anonymous;
            NotifyAuthenticationStateChanged(Task.FromResult(_current));
        }
    }

    public void NotifyUserLogout()
    {
        _current = Anonymous;
        NotifyAuthenticationStateChanged(Task.FromResult(_current));
    }
}
