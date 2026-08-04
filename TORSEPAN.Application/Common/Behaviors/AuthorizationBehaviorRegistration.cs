using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace TORSEPAN.Application.Common.Behaviors;

public static class AuthorizationBehaviorRegistration
{
    public static IServiceCollection AddAuthorizationBehavior(
        this IServiceCollection services)
    {
        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(AuthorizationBehavior<,>));

        return services;
    }
}