using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using TORSEPAN.Panel.Components;
using TORSEPAN.Panel.Services;
using TORSEPAN.Panel.Services.Api;
using TORSEPAN.Panel.Services.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthorizationCore();

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

app.Run();
