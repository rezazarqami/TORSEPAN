using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using TORSEPAN.Panel.Authentication;

namespace TORSEPAN.Panel.Services.Auth;

public sealed class AuthStateProvider(TokenStorage storage)
    : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await storage.GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
            return Anonymous;

        var identity = new ClaimsIdentity(
            JwtParser.ParseClaims(token),
            "jwt");

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyUserAuthentication()
        => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    public void NotifyUserLogout()
        => NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
}