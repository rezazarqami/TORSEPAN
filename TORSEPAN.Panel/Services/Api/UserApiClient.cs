using System.Net.Http.Json;
using TORSEPAN.Application.Auth.Commands.CreateUser;
using TORSEPAN.Application.Auth.Queries.GetUsers;

namespace TORSEPAN.Panel.Services.Api;

public class UserApiClient
{
    private readonly HttpClient _httpClient;

    public UserApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        var result = await _httpClient.GetFromJsonAsync<List<UserDto>>("/api/users");

        return result ?? new List<UserDto>();
    }

    public async Task<Guid> CreateUserAsync(CreateUserCommand command)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/users", command);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}