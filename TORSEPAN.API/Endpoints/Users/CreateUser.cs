using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TORSEPAN.Application.Auth.Commands.CreateUser;

namespace TORSEPAN.Api.Endpoints.Users;

public static class CreateUser
{
    public static IEndpointRouteBuilder MapCreateUser(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users", Handle);

        return app;
    }

    private static async Task<IResult> Handle(
        CreateUserCommand command,
        IMediator mediator)
    {
        var id = await mediator.Send(command);

        return Results.Ok(id);
    }
}