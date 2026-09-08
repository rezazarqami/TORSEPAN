using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.JSInterop;
using TORSEPAN.API.Controllers;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Panel.Services.Auth;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

static void Check(bool value, string message)
{
    if (!value) throw new Exception(message);
    Console.WriteLine("PASS " + message);
}

var services = new ServiceCollection().AddLogging().AddAuthorizationCore().BuildServiceProvider();
var provider = services.GetRequiredService<IAuthorizationPolicyProvider>();
var authorization = services.GetRequiredService<IAuthorizationService>();
var roles = new[] { "Administrator", "ProductionManager", "SalesAdmin", "Marketer", "Workshop", "Shaper", "Tuner", "Warehouse" };
async Task Verify(Type type, string? method, Func<string,bool> expected)
{
    var attributes = type.GetCustomAttributes<AuthorizeAttribute>().Cast<IAuthorizeData>().ToList();
    if(method is not null) attributes.AddRange(type.GetMethod(method)!.GetCustomAttributes<AuthorizeAttribute>());
    var policy = await AuthorizationPolicy.CombineAsync(provider, attributes) ?? throw new Exception("Missing access policy");
    foreach(var role in roles.Append("anonymous"))
    {
        var user = role == "anonymous" ? new ClaimsPrincipal(new ClaimsIdentity()) : new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.Role, role)},"test"));
        var allowed = (await authorization.AuthorizeAsync(user,null,policy)).Succeeded;
        if(allowed != (role != "anonymous" && expected(role))) throw new Exception($"Unexpected access: {type.Name}.{method} / {role}");
    }
    Console.WriteLine($"PASS {type.Name}.{method ?? "page"}: allowed and denied roles");
}
bool Sales(string r) => r is "Administrator" or "ProductionManager" or "SalesAdmin";
bool Marketing(string r) => r is "Administrator" or "ProductionManager" or "Marketer";
foreach(var method in new[]{"Sales","Sell","SellBulk","UpdateSale"}) await Verify(typeof(ProductionController),method,Sales);
foreach(var method in new[]{"Warehouse","WarehouseDetails","WarehouseGallery"}) await Verify(typeof(ProductionController),method,_=>true);
await Verify(typeof(AccountingController),null,Sales);
foreach(var method in typeof(MarketingController).GetMethods(BindingFlags.DeclaredOnly|BindingFlags.Public|BindingFlags.Instance)) await Verify(typeof(MarketingController),method.Name,Marketing);
await Verify(typeof(BowlsController),"ShipExportBowl",r=>Sales(r)||r=="Workshop");
await Verify(typeof(ProductionController),"Rollback",r=>r=="Administrator");
await Verify(typeof(BowlsController),"Rollback",r=>r=="Administrator");
await Verify(typeof(AuthController),"GetRoles",r=>r is "Administrator" or "ProductionManager");
await Verify(typeof(TORSEPAN.Panel.Components.Pages.Sales),null,Sales);
await Verify(typeof(TORSEPAN.Panel.Components.Pages.Accounting),null,Sales);
await Verify(typeof(TORSEPAN.Panel.Components.Pages.Marketing),null,Marketing);
await Verify(typeof(TORSEPAN.Panel.Components.Pages.WarehouseList),null,_=>true);
await using var db=new TORSEPANDbContext(new DbContextOptionsBuilder<TORSEPANDbContext>().UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused").Options);
var sql=db.GetService<IMigrator>().GenerateScript("20260903010000_AddWorkshopMessages","20260907010000_AddSalesAdminRole");
if(!sql.Contains("SalesAdmin")||!sql.Contains("ادمین فروش")||!sql.Contains("ON CONFLICT"))throw new Exception("Role migration missing");
Console.WriteLine("PASS sales administrator migration SQL");
var browserStorage = new RestoringBrowserStorage();
var authState = new AuthStateProvider(new TokenStorage(browserStorage));
Check(!await authState.RefreshAsync(), "temporary browser-storage failure requests an authentication retry");
Check(await authState.RefreshAsync(), "authentication retry succeeds when browser storage becomes available");
var soldHandpan = new Handpan(Guid.NewGuid(), "ROLLBACK-TEST");
soldHandpan.ChangeStage(ProductionStage.FinishedWarehouse);
soldHandpan.ChangeStatus(ProductionStatus.Completed);
soldHandpan.Sell("Test buyer", 100, "Test destination", Guid.NewGuid());
soldHandpan.ReturnToWarehouse();
Check(soldHandpan.Stage == ProductionStage.FinishedWarehouse &&
      soldHandpan.Status == ProductionStatus.Completed &&
      soldHandpan.SoldAt is null && soldHandpan.SoldByUserId is null &&
      soldHandpan.BuyerName is null && soldHandpan.SalePrice is null && soldHandpan.SaleDestination is null,
    "sale rollback clears sale data and restores warehouse state");

sealed class RestoringBrowserStorage : IJSRuntime
{
    private int _attempts;
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if (_attempts++ == 0) throw new JSException("Browser tab is still restoring");
        return ValueTask.FromResult(default(TValue)!);
    }
}
