using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TORSEPAN.Application.Auth.Queries.GetUsers;

namespace TORSEPAN.Api.Endpoints.Users;

public static class GetUsers
{
    public static IEndpointRouteBuilder MapGetUsers(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users", Handle);

        return app;
    }

    private static async Task<IResult> Handle(IMediator mediator)
    {
        var result = await mediator.Send(new GetUsersQuery());

        return Results.Ok(result);
    }
}