using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using TORSEPAN.Panel.Models;
using TORSEPAN.Panel.Services;
using TORSEPAN.Panel.Services.Api;
using TORSEPAN.Panel.Services.Auth;
using TORSEPAN.Panel.Components.Shared;
static class UiChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
 public static async Task Run(){var fixture=new FixtureApi();var auth=new FixtureAuth("Administrator");var services=new ServiceCollection();services.AddLogging();services.AddAuthorizationCore();services.AddCascadingAuthenticationState();services.AddSingleton<AuthenticationStateProvider>(auth);services.AddSingleton<IJSRuntime,FixtureJs>();services.AddSingleton<NavigationManager,FixtureNavigation>();services.AddSingleton(new HttpClient(fixture){BaseAddress=new Uri("https://fixture.invalid/api/")});services.AddScoped<TokenStorage>();services.AddScoped<ApiClient>();services.AddScoped<ProductionService>();services.AddScoped<AccountingService>();services.AddScoped<HandpanPhotoService>();services.AddScoped<SalesAttributionService>();services.AddScoped<BowlService>();services.AddScoped<MaterialService>();services.AddScoped<ScaleService>();services.AddScoped<PersonalWorkspaceService>();services.AddScoped<ProfitSharingService>();
 await using var provider=services.BuildServiceProvider();await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());var dir=Environment.GetEnvironmentVariable("PROFIT_PREVIEW_DIR");if(dir is not null)Directory.CreateDirectory(dir);
 await renderer.Dispatcher.InvokeAsync(async()=>{
 var stock=await renderer.RenderComponentAsync<WarehousePreview>(ParameterView.Empty);var page=WarehousePreview.Instance!;var row=((List<WarehouseHandpanDto>)page.Get("_items")!).First();await page.Call("BeginSale",row);page.Refresh();
 var html=WebUtility.HtmlDecode(stock.ToHtmlString());Check(html.Contains("سود خالص")&&html.Contains("دلار (USD)")&&!html.Contains("نوع فروش <strong>*"),"compact stock sale has optional profit and independent USD/toman selectors");if(dir is not null)await File.WriteAllTextAsync(Path.Combine(dir,"sale.html"),stock.ToHtmlString());page.Set("_isExportSale",true);page.Set("_leadSourceId",Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));page.Refresh();if(dir is not null)await File.WriteAllTextAsync(Path.Combine(dir,"sale-export.html"),stock.ToHtmlString());
 var profit=await renderer.RenderComponentAsync<ProfitPreview>(ParameterView.Empty);var panel=ProfitPreview.Instance!;html=WebUtility.HtmlDecode(profit.ToHtmlString());Check(html.Contains("شایان نجفی")&&html.Contains("7,000,000")&&html.Contains("130.00"),"member cards present separate toman and USD totals");if(dir is not null)await File.WriteAllTextAsync(Path.Combine(dir,"members.html"),profit.ToHtmlString());var member=((List<ProfitMemberDto>)panel.Get("_members")!).First();await panel.Call("Open",member);panel.Refresh();html=WebUtility.HtmlDecode(profit.ToHtmlString());Check(html.Contains("ساز 553")&&html.Contains("سود مبنا"),"tapping member opens per-instrument stage and profit breakdown");if(dir is not null)await File.WriteAllTextAsync(Path.Combine(dir,"profile.html"),profit.ToHtmlString());
 Check(html.Contains("profit-rule-2")&&html.Contains("profit-rule-3")&&html.Contains("profit-rule-5")&&html.Contains("profit-rule-6")&&html.Contains("profit-rule-7"),"all five requested manual stage percentages are rendered");
 var hall=((List<ProfitMemberDto>)panel.Get("_members")!).Single(x=>x.Name=="خدمات سالن");await panel.Call("Open",hall);panel.Refresh();html=WebUtility.HtmlDecode(profit.ToHtmlString());Check(html.Contains("profit-rule-100")&&html.Contains("مانده طلب")&&html.Contains("ثبت پرداخت خدمات سالن")&&html.Contains("400,000"),"hall percentage, payment form and remaining balances are visible in the fixed account");if(dir is not null)await File.WriteAllTextAsync(Path.Combine(dir,"hall.html"),profit.ToHtmlString());
 var localRules=((List<ProfitRuleDto>)panel.Get("_rules")!);localRules.Single(x=>x.Action==3).Percent=9;await panel.Call("SaveRules");Check((bool)panel.Get("_saved")!,"manual percentages save through the API service");
 fixture.FailMembers=HttpStatusCode.InternalServerError;await panel.Call("Load");panel.Refresh();html=WebUtility.HtmlDecode(profit.ToHtmlString());Check(html.Contains("profit-rule-2")&&html.Contains("ذخیره درصدها")&&!html.Contains("هنوز عضوی"),"member-list failure cannot hide working percentage inputs or claim an empty account");if(dir is not null)await File.WriteAllTextAsync(Path.Combine(dir,"members-error.html"),profit.ToHtmlString());
 fixture.FailMembers=null;fixture.FailRules=HttpStatusCode.NotFound;await panel.Call("Load");panel.Refresh();html=WebUtility.HtmlDecode(profit.ToHtmlString());Check(html.Contains("torsepan-api")&&html.Contains("شایان نجفی")&&!html.Contains("ذخیره درصدها")&&!html.Contains("جمع: 0"),"old API gives actionable version error without fake zero rules or a save button");if(dir is not null)await File.WriteAllTextAsync(Path.Combine(dir,"rules-error.html"),profit.ToHtmlString());
 fixture.FailRules=null;auth.SetRole("Tuner");var before=fixture.ProfitRequests;var denied=await renderer.RenderComponentAsync<ProfitSharing>();html=WebUtility.HtmlDecode(denied.ToHtmlString());Check(!html.Contains("profit-title")&&!html.Contains("profit-rule-")&&fixture.ProfitRequests==before,"ordinary members cannot see the profit screen or initiate profit API requests");auth.SetRole("Administrator");


 });}
}
public sealed class ProfitPreview:ProfitSharing{public static ProfitPreview? Instance;protected override async Task OnInitializedAsync(){await base.OnInitializedAsync();Instance=this;}public object? Get(string name)=>typeof(ProfitSharing).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(this);public async Task Call(string name,params object?[] args){var r=typeof(ProfitSharing).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(this,args);if(r is Task t)await t;}public void Refresh()=>StateHasChanged();}
