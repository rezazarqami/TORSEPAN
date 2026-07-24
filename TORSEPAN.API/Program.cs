using Microsoft.EntityFrameworkCore;
using TORSEPAN.API.Extensions;
using TORSEPAN.Application.DependencyInjection;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Infrastructure.DependencyInjection;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<TORSEPANDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Application
builder.Services.AddApplication();

// Infrastructure
builder.Services.AddInfrastructure();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBowlRepository, BowlRepository>();
builder.Services.AddScoped<IHandpanRepository, HandpanRepository>();
builder.Services.AddScoped<IHandpanAssemblyRepository, HandpanAssemblyRepository>();
builder.Services.AddScoped<IProductionEventRepository, ProductionEventRepository>();

// Unit Of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Controllers
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "TORSEPAN API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// Global Exception Middleware
app.UseGlobalExceptionMiddleware();

app.UseAuthorization();

app.MapControllers();

app.Run();