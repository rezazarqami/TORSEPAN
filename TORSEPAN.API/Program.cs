using System.Threading.RateLimiting;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using TORSEPAN.Application;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Infrastructure.DependencyInjection;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Infrastructure.Services;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;
using System.Reflection;
using TORSEPAN.API.Services;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;
await using (var vazirmatn = Assembly.GetExecutingAssembly()
    .GetManifestResourceStream("TORSEPAN.API.Assets.Vazirmatn-Regular.ttf")
    ?? throw new InvalidOperationException("Embedded Vazirmatn font was not found."))
{
    FontManager.RegisterFont(vazirmatn);
}

builder.Services.AddControllers();
builder.Services.AddScoped<MessagePushKeyStore>();
builder.Services.AddHostedService<MessagePushWorker>();
builder.Services.AddHttpClient<IMessagePushTransport, WebMessagePushTransport>().ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddScoped<TORSEPAN.API.Controllers.ManagementReportsController>();
builder.Services.AddHttpClient<GuaranteeServiceClient>((services, client) =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri((configuration["GuaranteeService:BaseUrl"] ?? "http://torsepan-guarantee:8080").TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(12);
});
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

builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = 429;
    options.AddPolicy("account-security", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
});
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
            user.UserName.Trim().ToUpper() == normalizedUserName);

        if (bootstrapUser is not null && builder.Configuration.GetValue<bool>("BootstrapAdmin:ResetExistingPassword"))
        {
            bootstrapUser.SetPassword(bootstrapPassword);
            bootstrapUser.Activate();
            await dbContext.SaveChangesAsync();
            app.Logger.LogInformation(
                "Bootstrap password reset was applied to the configured user.");
        }
        else if (bootstrapUser is null && !await dbContext.Users.AnyAsync())
        {
            var administratorRole = await dbContext.Roles.FirstOrDefaultAsync(role =>
                role.Name == "Administrator");

            if (administratorRole is null)
            {
                app.Logger.LogError(
                    "Bootstrap administrator could not be created because the Administrator role was not found.");
            }
            else
            {
                bootstrapUser = new User(
                    bootstrapUserName.Trim(),
                    bootstrapUserName.Trim());
                bootstrapUser.SetPassword(bootstrapPassword);
                bootstrapUser.UserRoles.Add(
                    new UserRole(bootstrapUser.Id, administratorRole.Id));

                dbContext.Users.Add(bootstrapUser);
                await dbContext.SaveChangesAsync();

                app.Logger.LogInformation(
                    "Bootstrap administrator was created from the configured environment variables.");
            }
        }
    }
    else
    {
        app.Logger.LogWarning(
            "Bootstrap password reset was skipped because its environment variables are missing.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", (DatabaseBackupStatus backup, TelegramAlertStatus alerts, OrderReminderStatus orderReminders, IConfiguration configuration) => Results.Ok(new
{
    status = "healthy",
    telegram = new
    {
        relayConfigured = !string.IsNullOrWhiteSpace(configuration["Telegram:RelayUrl"]),
        backupRelayConfigured = !string.IsNullOrWhiteSpace(configuration["Telegram:BackupRelayUrl"]) ||
            !string.IsNullOrWhiteSpace(configuration["Telegram:RelayUrl"]),
        relaySecretConfigured = !string.IsNullOrWhiteSpace(configuration["Telegram:RelaySecret"]),
        inventoryAlert = alerts.Snapshot()
    },
    databaseBackup = new
    {
        backup.State,
        backup.Stage,
        backup.Mode,
        backup.LastAttemptUtc,
        backup.LastSuccessUtc,
        backup.Error
    },
    orderReminders = orderReminders.Snapshot()
}));

app.Run();
