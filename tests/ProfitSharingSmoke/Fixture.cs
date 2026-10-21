using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using TORSEPAN.Panel.Components.Pages;
using TORSEPAN.Panel.Models;
using TORSEPAN.Panel.Services;
using TORSEPAN.Panel.Services.Api;
using TORSEPAN.Panel.Services.Auth;

public sealed class WarehousePreview:WarehouseList
{
    public static WarehousePreview? Instance;protected override async Task OnInitializedAsync(){await base.OnInitializedAsync();Instance=this;}
    public object? Get(string name)=>typeof(WarehouseList).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(this);
    public void Set(string name,object? value)=>typeof(WarehouseList).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(this,value);
    public async Task Call(string name,params object?[] args){var r=typeof(WarehouseList).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(this,args);if(r is Task t)await t;}
    public void Refresh()=>StateHasChanged();
}
public sealed class QcPreview:Dimpling
{
    public static QcPreview? Instance;protected override async Task OnInitializedAsync(){await base.OnInitializedAsync();Instance=this;Set("_bowl",new DimpleBowlDto{ProductionCode="QC-FIXTURE",Stage=14,BowlType=1,HandpanId=Guid.NewGuid()});Set("_productionCode","QC-FIXTURE");Set("_qualityWarehouses",InstrumentWarehouseDto.Defaults());}
    public void Refresh()=>StateHasChanged();
    public object? Get(string name)=>typeof(Dimpling).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(this);
    public void Set(string name,object? value)=>typeof(Dimpling).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(this,value);
    public async Task Call(string name,params object?[] args){var r=typeof(Dimpling).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(this,args);if(r is Task t)await t;}
}
sealed class FixtureAuth(string role):AuthenticationStateProvider{public override Task<AuthenticationState> GetAuthenticationStateAsync()=>Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.Role,role)},"fixture"))));}
sealed class FixtureJs:IJSRuntime{public ValueTask<T> InvokeAsync<T>(string id,object?[]? args)=>ValueTask.FromResult(default(T)!);public ValueTask<T> InvokeAsync<T>(string id,CancellationToken ct,object?[]? args)=>InvokeAsync<T>(id,args);}
sealed class FixtureNavigation:NavigationManager{public FixtureNavigation(){Initialize("https://fixture.invalid/","https://fixture.invalid/warehouse/list");}protected override void NavigateToCore(string uri,bool forceLoad){}}
sealed class FixtureApi:HttpMessageHandler
{
    public int Transfers,Renames,QcRequests,LastQualityWarehouse; public int? LastExportLocation;
    readonly List<InstrumentWarehouseDto> warehouses=InstrumentWarehouseDto.Defaults();
    public readonly List<WarehouseHandpanDto> Stock=new[]{("ST-00281","D Kurd 9",1), ("ST-00284","D Amara 13",1), ("ST-00295","E Sabye 9",1), ("ST-00310","D Kurd 14",2), ("ST-00326","C Aegean 10",2), ("ST-00402","F Low Pygmy 16",3)}.Select((x,i)=>new WarehouseHandpanDto{HandpanId=Guid.NewGuid(),SerialNumber=x.Item1,TopBowlCode=x.Item1,BottomBowlCode="SB-"+(20000+i),MaterialName=i%2==0?"Ember Steel":"Stainless Steel",ScaleName=x.Item2,QualityWarehouseId=x.Item3,Version=Guid.NewGuid(),WarehouseEntryDate=new DateTime(2026,10,10,10,0,0),Notes=i==1?["انتخاب‌شده برای سفارش مشتری"]:[]}).ToList();
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
    {
        object data=Array.Empty<object>();var path=request.RequestUri!.AbsolutePath;
        if(path.EndsWith("/profit-sharing/rules"))data=new[]{new ProfitRuleDto{Action=2,Percent=2,Version=Guid.NewGuid()},new ProfitRuleDto{Action=3,Percent=7,Version=Guid.NewGuid()},new ProfitRuleDto{Action=5,Percent=1,Version=Guid.NewGuid()},new ProfitRuleDto{Action=6,Percent=13,Version=Guid.NewGuid()},new ProfitRuleDto{Action=7,Percent=13,Version=Guid.NewGuid()},new ProfitRuleDto{Action=13,Percent=0,Version=Guid.NewGuid()}};
        else if(path.EndsWith("/profit-sharing/members"))data=new[]{new ProfitMemberDto{UserId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),Name="شایان نجفی",Toman=7000000,Usd=130,Instruments=2}};
        else if(path.Contains("/profit-sharing/members/"))data=new[]{new ProfitEntryDto{Code="553",Action=3,RolePercent=7,NetProfit=100000000,Amount=7000000,Currency=1,SoldAt=DateTime.UtcNow}};
        else if(path.EndsWith("/instrument-warehouses"))data=warehouses;
        else if(path.Contains("/instrument-warehouses/")&&request.Method==HttpMethod.Put){Renames++;var d=await request.Content!.ReadFromJsonAsync<JsonElement>(ct);var id=int.Parse(path.Split('/').Last());var item=warehouses.Single(x=>x.Id==id);item.Name=d.GetProperty("name").GetString()!;item.Version=Guid.NewGuid();data=item;}
        else if(path.EndsWith("/instrument-warehouses/transfer")){Transfers++;var d=await request.Content!.ReadFromJsonAsync<JsonElement>(ct);var id=d.GetProperty("warehouseId").GetInt32();var rows=d.GetProperty("items").EnumerateArray().Select(x=>Stock.Single(s=>s.HandpanId==x.GetProperty("handpanId").GetGuid())).ToList();foreach(var row in rows){row.QualityWarehouseId=id;row.Version=Guid.NewGuid();}data=rows.Select(x=>new WarehouseMoveResult{HandpanId=x.HandpanId,QualityWarehouseId=x.QualityWarehouseId,Version=x.Version}).ToList();}
        else if(path.EndsWith("/production/warehouse"))data=Stock;
        else if(path.EndsWith("/qc/complete")){QcRequests++;var d=await request.Content!.ReadFromJsonAsync<JsonElement>(ct);LastQualityWarehouse=d.GetProperty("qualityWarehouseId").ValueKind==JsonValueKind.Null?1:d.GetProperty("qualityWarehouseId").GetInt32();LastExportLocation=d.GetProperty("exportWarehouseLocation").ValueKind==JsonValueKind.Null?null:d.GetProperty("exportWarehouseLocation").GetInt32();data=new DimpleBowlDto{ProductionCode="QC-FIXTURE",Stage=16,BowlType=1};}
        else if(path.EndsWith("/sales-attribution/options"))data=new SalesAttributionOptionsDto{Sources=[new(){Id=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),Name="معرف",RequiresReferrer=true}],Referrers=[new(){Id=Guid.NewGuid(),Name="معرف نمونه"}]};
        return new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(data)};
    }
}
