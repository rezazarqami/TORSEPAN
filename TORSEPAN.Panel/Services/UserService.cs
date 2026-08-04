using TORSEPAN.Application.Auth.Commands.CreateUser;
using TORSEPAN.Application.Auth.Queries.GetUsers;
using TORSEPAN.Panel.Services.Api;

namespace TORSEPAN.Panel.Services;

public class UserService : IUserService
{
    private readonly UserApiClient _api;

    public UserService(UserApiClient api)
    {
        _api = api;
    }

    public Task<List<UserDto>> GetUsersAsync()
    {
        return _api.GetUsersAsync();
    }

    public Task<Guid> CreateUserAsync(CreateUserCommand command)
    {
        return _api.CreateUserAsync(command);
    }
}