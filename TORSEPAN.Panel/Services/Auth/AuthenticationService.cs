using TORSEPAN.Panel.Models;
using TORSEPAN.Panel.Services.Api;

namespace TORSEPAN.Panel.Services.Auth;

public sealed class AuthenticationService(
    ApiClient api,
    TokenStorage storage,
    AuthStateProvider provider)
{
    public async Task<bool> LoginAsync(LoginRequest request)
    {
        var result = await api.PostAsync<LoginRequest, LoginResponse>(
            ApiEndpoints.Login,
            request);

        if (result is null)
            return false;

        await storage.SaveAsync(
            result.AccessToken,
            result.RefreshToken);

        provider.NotifyUserAuthentication();

        return true;
    }

    public async Task LogoutAsync()
    {
        await storage.ClearAsync();

        provider.NotifyUserLogout();
    }
}