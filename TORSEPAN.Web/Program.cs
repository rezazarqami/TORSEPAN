using Microsoft.EntityFrameworkCore;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<TORSEPANDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("TORSEPANDatabase")));

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();