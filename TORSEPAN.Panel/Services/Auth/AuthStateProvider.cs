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

    public async Task<bool> RefreshAsync()
    {
        string? token;
        try
        {
            token = await storage.GetAccessTokenAsync();
        }
        catch
        {
            // Browser storage can be briefly unavailable while a mobile tab is
            // being restored. Let the caller retry instead of treating that as
            // a signed-out user.
            return false;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            SetCurrent(Anonymous);
            return true;
        }

        try
        {
            var identity = new ClaimsIdentity(
                JwtParser.ParseClaims(token),
                "jwt");
            SetCurrent(new AuthenticationState(new ClaimsPrincipal(identity)));
            return true;
        }
        catch
        {
            // A malformed saved token is a real anonymous state, not a
            // temporary browser restoration failure.
            SetCurrent(Anonymous);
            return true;
        }
    }

    public void NotifyUserLogout()
    {
        SetCurrent(Anonymous);
    }

    private void SetCurrent(AuthenticationState state)
    {
        _current = state;
        NotifyAuthenticationStateChanged(Task.FromResult(_current));
    }
}
