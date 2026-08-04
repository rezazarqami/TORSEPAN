using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Production;
using TORSEPAN.Infrastructure.Authentication;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Infrastructure.Persistence.Repositories;
using TORSEPAN.Infrastructure.Services;

namespace TORSEPAN.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<TORSEPANDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection")));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],

                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),

                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();

        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IHandpanRepository, HandpanRepository>();
        services.AddScoped<IHandpanAssemblyRepository, HandpanAssemblyRepository>();
        services.AddScoped<IBowlRepository, BowlRepository>();
        services.AddScoped<IMaterialRepository, MaterialRepository>();
        services.AddScoped<IProductionEventRepository, ProductionEventRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IProductionEngine, ProductionEngine>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IUserContext, UserContext>();

        services.AddScoped<ICodeGenerator, ProductionCodeGenerator>();

        services.AddScoped<IBowlQueryService, BowlQueryService>();

        return services;
    }
}