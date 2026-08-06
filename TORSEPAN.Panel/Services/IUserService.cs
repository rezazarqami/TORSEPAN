using TORSEPAN.Application.Auth.Commands.CreateUser;
using TORSEPAN.Application.Auth.Commands.UpdateUser;
using TORSEPAN.Application.Auth.Queries.GetUsers;
using TORSEPAN.Application.Auth.Queries.GetRoles;
using UserDetailsDto = TORSEPAN.Application.Auth.Queries.GetUserById.UserDto;

namespace TORSEPAN.Panel.Services;

public interface IUserService
{
    Task<List<UserDto>> GetUsersAsync();
    Task<Guid> CreateUserAsync(CreateUserCommand command);
    Task<List<RoleDto>> GetRolesAsync();
    Task<UserDetailsDto?> GetUserAsync(Guid userId);
    Task UpdateUserAsync(UpdateUserCommand command);
}
