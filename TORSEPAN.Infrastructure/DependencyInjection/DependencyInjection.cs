using Microsoft.Extensions.DependencyInjection;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Production;
using TORSEPAN.Infrastructure.Services;

namespace TORSEPAN.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ICodeGenerator, ProductionCodeGenerator>();

        services.AddScoped<IBowlQueryService, BowlQueryService>();

        services.AddScoped<IProductionEngine, ProductionEngine>();

        return services;
    }
}