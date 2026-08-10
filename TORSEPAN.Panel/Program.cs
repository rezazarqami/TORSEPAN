using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using TORSEPAN.Panel.Components;
using TORSEPAN.Panel.Services;
using TORSEPAN.Panel.Services.Api;
using TORSEPAN.Panel.Services.Auth;
using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(10);
        options.DisconnectedCircuitMaxRetained = 200;
        options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
        options.MaxBufferedUnacknowledgedRenderBatches = 20;
    })
    .AddHubOptions(options =>
    {
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.MaximumParallelInvocationsPerClient = 2;
    });

builder.Services.AddAuthorizationCore();
builder.Services.AddHttpClient();

builder.Services.AddCascadingAuthenticationState();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddScoped<TokenStorage>();
builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();

var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Set Api__BaseUrl to the public API URL.");
apiBaseUrl = apiBaseUrl.TrimEnd('/');

if (!apiBaseUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
{
    apiBaseUrl = $"{apiBaseUrl}/api";
}

apiBaseUrl = $"{apiBaseUrl}/";

builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddScoped<ApiClient>(serviceProvider =>
{
    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    var tokenStorage = serviceProvider.GetRequiredService<TokenStorage>();

    return new ApiClient(
        httpClientFactory.CreateClient("Api"),
        tokenStorage);
});

builder.Services.AddScoped<IAuthService, AuthenticationService>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<UserApiClient>();

builder.Services.AddScoped<ProductionService>();
builder.Services.AddScoped<HandpanService>();
builder.Services.AddScoped<BowlService>();
builder.Services.AddScoped<MaterialService>();
builder.Services.AddScoped<ScaleService>();
builder.Services.AddScoped<ReportService>();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapPost("/api/internal/telegram-inventory-alert", async (
    HttpRequest request, TelegramRelayRequest alert, IConfiguration configuration,
    IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var expectedSecret = configuration["TelegramRelay:Secret"];
    if (string.IsNullOrWhiteSpace(expectedSecret) ||
        request.Headers["X-Relay-Secret"] != expectedSecret)
        return Results.Unauthorized();

    var token = configuration["TelegramRelay:BotToken"];
    var chatId = configuration["TelegramRelay:ChatId"];
    if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chatId))
        return Results.Problem("Telegram relay is not configured.");

    var text = $"âš ï¸ Ù‡Ø´Ø¯Ø§Ø± Ù…ÙˆØ¬ÙˆØ¯ÛŒ Ø§Ù†Ø¨Ø§Ø± Ù…ÙˆØ§Ø¯ Ø§ÙˆÙ„ÛŒÙ‡\n{alert.ItemName} - {alert.StockType}\nÙ…ÙˆØ¬ÙˆØ¯ÛŒ ÙØ¹Ù„ÛŒ: {alert.Quantity}\nØ­Ø¯ Ù‡Ø´Ø¯Ø§Ø±: {alert.Threshold}";
    var response = await httpClientFactory.CreateClient().PostAsJsonAsync(
        $"https://api.telegram.org/bot{token}/sendMessage",
        new { chat_id = chatId, text }, cancellationToken);
    return response.IsSuccessStatusCode ? Results.Ok() : Results.StatusCode((int)response.StatusCode);
}).DisableAntiforgery();

app.Run();

public sealed record TelegramRelayRequest(string ItemName, string StockType, int Quantity, int Threshold);

