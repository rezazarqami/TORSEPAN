using TORSEPAN.Application.Auth.Commands.CreateUser;
using TORSEPAN.Application.Auth.Commands.UpdateUser;
using TORSEPAN.Application.Auth.Queries.GetUsers;
using TORSEPAN.Application.Auth.Queries.GetRoles;
using UserDetailsDto = TORSEPAN.Application.Auth.Queries.GetUserById.UserDto;

namespace TORSEPAN.Panel.Services.Api;

public class UserApiClient
{
    private readonly ApiClient _apiClient;

    public UserApiClient(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        var result = await _apiClient.GetAsync<List<UserDto>>("auth/users");
        return result ?? new List<UserDto>();
    }

    public async Task<Guid> CreateUserAsync(CreateUserCommand command)
    {
        return await _apiClient.PostAsync<CreateUserCommand, Guid>("auth/users", command);
    }

    public async Task<List<RoleDto>> GetRolesAsync()
    {
        var result = await _apiClient.GetAsync<List<RoleDto>>("auth/roles");
        return result ?? new List<RoleDto>();
    }

    public Task<UserDetailsDto?> GetUserAsync(Guid userId)
    {
        return _apiClient.GetAsync<UserDetailsDto>($"auth/users/{userId}");
    }

    public async Task UpdateUserAsync(UpdateUserCommand command)
    {
        await _apiClient.PutAsync<UpdateUserCommand, object?>(
            $"auth/users/{command.UserId}", command);
    }
}
