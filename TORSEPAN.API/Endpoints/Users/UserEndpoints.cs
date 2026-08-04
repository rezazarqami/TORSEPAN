using Microsoft.AspNetCore.Routing;

namespace TORSEPAN.Api.Endpoints.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGetUsers();
        app.MapCreateUser();

        return app;
    }
}