using TORSEPAN.Application.Auth.Commands.CreateUser;
using TORSEPAN.Application.Auth.Commands.UpdateUser;
using TORSEPAN.Application.Auth.Queries.GetUsers;
using TORSEPAN.Application.Auth.Queries.GetRoles;
using TORSEPAN.Panel.Services.Api;
using UserDetailsDto = TORSEPAN.Application.Auth.Queries.GetUserById.UserDto;

namespace TORSEPAN.Panel.Services;

public class UserService : IUserService
{
    private readonly UserApiClient _api;

    public UserService(UserApiClient api)
    {
        _api = api;
    }

    public Task<List<UserDto>> GetUsersAsync() => _api.GetUsersAsync();

    public Task<Guid> CreateUserAsync(CreateUserCommand command) =>
        _api.CreateUserAsync(command);

    public Task<List<RoleDto>> GetRolesAsync() => _api.GetRolesAsync();

    public Task<UserDetailsDto?> GetUserAsync(Guid userId) =>
        _api.GetUserAsync(userId);

    public Task UpdateUserAsync(UpdateUserCommand command) =>
        _api.UpdateUserAsync(command);
}
