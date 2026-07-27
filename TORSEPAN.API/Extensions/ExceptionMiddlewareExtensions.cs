using Microsoft.AspNetCore.Builder;
using TORSEPAN.API.Middleware;

namespace TORSEPAN.API.Extensions;

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionMiddleware(
        this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionMiddleware>();
    }
}