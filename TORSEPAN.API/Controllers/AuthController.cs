using MediatR;
using Microsoft.AspNetCore.Mvc;
using TORSEPAN.API.Contracts.Auth;
using TORSEPAN.Application.Auth.Commands.CreateUser;
using TORSEPAN.Application.Auth.Commands.DeleteUser;
using TORSEPAN.Application.Auth.Commands.RefreshLogin;
using TORSEPAN.Application.Auth.Commands.UpdateUser;
using TORSEPAN.Application.Auth.Commands.UpdateUserStatus;
using TORSEPAN.Application.Auth.Queries.GetActiveUsers;
using TORSEPAN.Application.Auth.Queries.GetInactiveUsers;
using TORSEPAN.Application.Auth.Queries.GetUserById;
using TORSEPAN.Application.Auth.Queries.GetUserByUsername;
using TORSEPAN.Application.Auth.Queries.GetUsers;
using TORSEPAN.Application.Auth.Queries.GetUsersPaged;
using TORSEPAN.Application.Auth.Queries.Login;
using TORSEPAN.Application.Auth.Queries.SearchUsers;
using TORSEPAN.Application.Auth.Queries.UserCount;
using TORSEPAN.Application.Auth.Queries.UserExists;
using TORSEPAN.Application.Auth.Queries.UserNameExists;
using TORSEPAN.Application.Auth.Queries.UserStatistics;
using TORSEPAN.Application.Auth.Queries.UserSummary;

using LoginResponse = TORSEPAN.Application.Auth.Queries.Login.LoginResponse;

namespace TORSEPAN.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginQuery query)
    {
        return Ok(await _mediator.Send(query));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshLoginResponse>> Refresh(
        [FromBody] RefreshTokenRequest request)
    {
        return Ok(await _mediator.Send(
            new RefreshLoginCommand(request.RefreshToken)));
    }

    [HttpPost("users")]
    public async Task<ActionResult<Guid>> CreateUser([FromBody] CreateUserCommand command)
    {
        return Ok(await _mediator.Send(command));
    }

    [HttpPut("users/{id:guid}")]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateUserCommand command)
    {
        await _mediator.Send(command with { UserId = id });
        return NoContent();
    }

    [HttpPut("users/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] bool isActive)
    {
        await _mediator.Send(new UpdateUserStatusCommand(id, isActive));
        return NoContent();
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<TORSEPAN.Application.Auth.Queries.GetUsers.UserDto>>> GetUsers()
    {
        return Ok(await _mediator.Send(new GetUsersQuery()));
    }

    [HttpGet("users/paged")]
    public async Task<ActionResult<GetUsersPagedResponse>> GetUsersPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        return Ok(await _mediator.Send(
            new GetUsersPagedQuery(pageNumber, pageSize)));
    }

    [HttpGet("users/search")]
    public async Task<ActionResult<List<TORSEPAN.Application.Auth.Queries.SearchUsers.UserDto>>> SearchUsers(
        [FromQuery] string keyword)
    {
        return Ok(await _mediator.Send(new SearchUsersQuery(keyword)));
    }

    [HttpGet("users/active")]
    public async Task<ActionResult<List<TORSEPAN.Application.Auth.Queries.GetActiveUsers.UserDto>>> GetActiveUsers()
    {
        return Ok(await _mediator.Send(new GetActiveUsers()));
    }

    [HttpGet("users/inactive")]
    public async Task<ActionResult<List<TORSEPAN.Application.Auth.Queries.GetInactiveUsers.UserDto>>> GetInactiveUsers()
    {
        return Ok(await _mediator.Send(new GetInactiveUsers()));
    }

    [HttpGet("users/{id:guid}")]
    public async Task<ActionResult<TORSEPAN.Application.Auth.Queries.GetUserById.UserDto>> GetUser(Guid id)
    {
        return Ok(await _mediator.Send(new GetUserByIdQuery(id)));
    }

    [HttpGet("users/by-username/{userName}")]
    public async Task<ActionResult<TORSEPAN.Application.Auth.Queries.GetUserByUsername.UserDto>> GetByUserName(string userName)
    {
        var user = await _mediator.Send(new GetUserByUsernameQuery(userName));

        if (user is null)
            return NotFound();

        return Ok(user);
    }

    [HttpGet("users/{id:guid}/exists")]
    public async Task<ActionResult<bool>> Exists(Guid id)
    {
        return Ok(await _mediator.Send(new UserExistsQuery(id)));
    }

    [HttpGet("users/exists/{userName}")]
    public async Task<ActionResult<bool>> UserNameExists(string userName)
    {
        return Ok(await _mediator.Send(new UserNameExistsQuery(userName)));
    }

    [HttpGet("users/count")]
    public async Task<ActionResult<UserCountResponse>> GetUserCount()
    {
        return Ok(await _mediator.Send(new UserCountQuery()));
    }

    [HttpGet("users/statistics")]
    public async Task<ActionResult<UserStatisticsResponse>> GetStatistics()
    {
        return Ok(await _mediator.Send(new UserStatisticsQuery()));
    }

    [HttpGet("users/summary")]
    public async Task<ActionResult<UserSummaryResponse>> GetSummary()
    {
        return Ok(await _mediator.Send(new UserSummaryQuery()));
    }

    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        await _mediator.Send(new DeleteUserCommand(id));
        return NoContent();
    }
}