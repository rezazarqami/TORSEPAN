using Microsoft.AspNetCore.Components.Authorization;
using TORSEPAN.Application.Auth.Commands.Login;
using TORSEPAN.Panel.Authentication;
using TORSEPAN.Panel.Services.Api;

namespace TORSEPAN.Panel.Services.Auth;

public sealed class AuthenticationService : IAuthService
{
    private readonly ApiClient _apiClient;
    private readonly TokenStorage _tokenStorage;
    private readonly AuthStateProvider _authStateProvider;

    private LoginResult? _currentUser;

    public AuthenticationService(
        ApiClient apiClient,
        TokenStorage tokenStorage,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _apiClient = apiClient;
        _tokenStorage = tokenStorage;
        _authStateProvider = (AuthStateProvider)authenticationStateProvider;
    }

    public bool IsAuthenticated =>
        _currentUser is not null && _currentUser.Success;

    public string? Token =>
        _currentUser?.Token;

    public string? UserName =>
        _currentUser?.UserName;

    public string? FullName =>
        _currentUser?.FullName;

    public IReadOnlyList<string> Roles =>
        _currentUser?.Roles ?? [];

    public async Task<LoginResult> LoginAsync(LoginCommand command)
    {
        _currentUser = await _apiClient.PostAsync<LoginCommand, LoginResult>(
            ApiEndpoints.Login,
            command);

        if (_currentUser is not null &&
            _currentUser.Success &&
            !string.IsNullOrWhiteSpace(_currentUser.Token))
        {
            await _tokenStorage.SaveAccessTokenAsync(_currentUser.Token);

            _apiClient.SetBearerToken(_currentUser.Token);

            await _authStateProvider.RefreshAsync();
        }

        return _currentUser ?? new LoginResult
        {
            Success = false
        };
    }

    public async Task LogoutAsync()
    {
        _currentUser = null;

        await _tokenStorage.ClearAsync();

        _apiClient.SetBearerToken(null);

        _authStateProvider.NotifyUserLogout();
    }
}