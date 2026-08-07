using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using TORSEPAN.Application;
using TORSEPAN.Infrastructure.DependencyInjection;
using TORSEPAN.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
});

builder.Services.AddApplication();

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TORSEPANDbContext>();
    await dbContext.Database.MigrateAsync();

    var bootstrapUserName = builder.Configuration["BootstrapAdmin:UserName"];
    var bootstrapPassword = builder.Configuration["BootstrapAdmin:Password"];

    if (!string.IsNullOrWhiteSpace(bootstrapUserName) &&
        !string.IsNullOrWhiteSpace(bootstrapPassword))
    {
        var normalizedUserName = bootstrapUserName.Trim().ToUpper();
        var bootstrapUser = await dbContext.Users.FirstOrDefaultAsync(user =>
            user.UserName.ToUpper() == normalizedUserName);

        if (bootstrapUser is not null)
        {
            bootstrapUser.SetPassword(bootstrapPassword);
            bootstrapUser.Activate();
            await dbContext.SaveChangesAsync();
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
