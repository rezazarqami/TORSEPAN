using TORSEPAN.Application.Auth.Commands.CreateUser;
using TORSEPAN.Application.Auth.Queries.GetUsers;

namespace TORSEPAN.Panel.Services;

public interface IUserService
{
    Task<List<UserDto>> GetUsersAsync();

    Task<Guid> CreateUserAsync(CreateUserCommand command);
}