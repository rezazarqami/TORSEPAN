using Microsoft.AspNetCore.Components.Authorization;
using TORSEPAN.Panel.Components;
using TORSEPAN.Panel.Services;
using TORSEPAN.Panel.Services.Api;
using TORSEPAN.Panel.Services.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthorizationCore();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<TokenStorage>();
builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();

builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7081/");
});

builder.Services.AddScoped<IAuthService, AuthenticationService>();

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<UserApiClient>();

builder.Services.AddScoped<ProductionService>();
builder.Services.AddScoped<HandpanService>();
builder.Services.AddScoped<BowlService>();
builder.Services.AddScoped<MaterialService>();
builder.Services.AddScoped<ReportService>();

var app = builder.Build();

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

app.Run();