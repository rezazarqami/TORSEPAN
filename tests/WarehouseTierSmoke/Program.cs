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

if(!args.Contains("--ui-only"))await BackendChecks.Run();
var preview=Environment.GetEnvironmentVariable("WAREHOUSE_PREVIEW_DIR");if(preview is not null)Directory.CreateDirectory(preview);
foreach(var role in new[]{"Administrator","QualityControl","Tuner"})
{
    var api=new FixtureApi();var services=new ServiceCollection();services.AddLogging();services.AddAuthorizationCore();services.AddCascadingAuthenticationState();services.AddSingleton<AuthenticationStateProvider>(new FixtureAuth(role));services.AddSingleton<IJSRuntime,FixtureJs>();services.AddSingleton<NavigationManager,FixtureNavigation>();services.AddScoped<TokenStorage>();services.AddSingleton(new HttpClient(api){BaseAddress=new Uri("https://fixture.invalid/api/")});services.AddScoped<ApiClient>();services.AddScoped<ProductionService>();services.AddScoped<AccountingService>();services.AddScoped<HandpanPhotoService>();services.AddScoped<SalesAttributionService>();services.AddScoped<BowlService>();services.AddScoped<MaterialService>();services.AddScoped<ScaleService>();
    await using var provider=services.BuildServiceProvider();await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
    await renderer.Dispatcher.InvokeAsync(async()=>
    {
        var rendered=await renderer.RenderComponentAsync<WarehousePreview>(ParameterView.Empty);var instance=WarehousePreview.Instance!;string Html()=>WebUtility.HtmlDecode(rendered.ToHtmlString());
        Check(Html().Contains("Economy")&&Html().Contains("Studio")&&Html().Contains("Signature")&&Html().Split("role=\"tab\"").Length-1==3,role+" sees exactly three warehouse tabs");
        Check(Html().Contains("ST-00281")&&!Html().Contains("ST-00310"),role+" initial inventory shows Economy only");
        Check(Html().Contains("مدیریت انبارها")== (role=="Administrator"),role+" naming controls stay in the administrator overflow menu");
        Check(Html().Contains("انتقال به انبار دیگر")== (role is "Administrator" or "QualityControl"),role+" transfer controls follow stock permissions");
        if(preview is not null&&role=="Administrator")await File.WriteAllTextAsync(Path.Combine(preview,"warehouse-economy.html"),rendered.ToHtmlString());
        if(role=="Administrator"){
            var row=((List<WarehouseHandpanDto>)instance.Get("_items")!).First();
            ((HashSet<Guid>)instance.Get("_expanded")!).Add(row.HandpanId);((HashSet<Guid>)instance.Get("_detailsLoaded")!).Add(row.HandpanId);
            row.Operations=[new(){Operation="دیمپل",PerformedBy="فؤاد احمدفیض",PerformedAt=DateTime.UtcNow},new(){Operation="شیپ",PerformedBy="محمدرضا رنجبر",PerformedAt=DateTime.UtcNow},new(){Operation="فاین تیون",PerformedBy="علیرضا ضرغامی",PerformedAt=DateTime.UtcNow}];row.PackagingItems=["روغن","دستمال","مثلثی"];instance.Refresh();
            if(preview is not null)await File.WriteAllTextAsync(Path.Combine(preview,"warehouse-details.html"),rendered.ToHtmlString());
            ((HashSet<Guid>)instance.Get("_expanded")!).Clear();instance.Refresh();
        }
        await instance.Call("SelectWarehouse",2);instance.Refresh();Check(Html().Contains("ST-00310")&&!Html().Contains("ST-00281"),role+" tab switching isolates Studio stock");
        if(preview is not null&&role=="Administrator")await File.WriteAllTextAsync(Path.Combine(preview,"warehouse-studio.html"),rendered.ToHtmlString());
        await instance.Call("SelectWarehouse",3);instance.Refresh();Check(Html().Contains("ST-00402")&&!Html().Contains("ST-00310"),role+" Signature has its own inventory");
        if(preview is not null&&role=="Administrator")await File.WriteAllTextAsync(Path.Combine(preview,"warehouse-signature.html"),rendered.ToHtmlString());
        await instance.Call("SelectWarehouse",1);instance.Refresh();
        if(role=="Administrator")
        {
            await instance.Call("OpenWarehouseManagement");instance.Refresh();Check(Html().Contains("warehouse-settings-title")&&api.Renames==0,"opening naming manager does not mutate data");if(preview is not null)await File.WriteAllTextAsync(Path.Combine(preview,"warehouse-management.html"),rendered.ToHtmlString());
            var names=(Dictionary<int,string>)instance.Get("_warehouseNames")!;names[2]="Atelier";await instance.Call("RenameWarehouseAsync",2);instance.Refresh();Check(Html().Contains("Atelier")&&api.Renames==1,"saved label updates tabs without changing stable warehouse identity");await instance.Call("CloseWarehouseManagement");
        }
        if(role is "Administrator" or "QualityControl")
        {
            var items=(List<WarehouseHandpanDto>)instance.Get("_items")!;var item=items.First(x=>x.QualityWarehouseId==1);await instance.Call("BeginTransfer",item);instance.Refresh();Check(api.Transfers==0&&Html().Contains("warehouse-transfer-title"),"transfer opens an in-app destination dialog before writing");if(preview is not null&&role=="Administrator")await File.WriteAllTextAsync(Path.Combine(preview,"warehouse-transfer.html"),rendered.ToHtmlString());await instance.Call("CancelTransfer");instance.Refresh();Check(api.Transfers==0&&!Html().Contains("warehouse-transfer-title"),"cancel transfer performs no request");
            await instance.Call("BeginTransfer",item);instance.Set("_transferWarehouseId",2);await instance.Call("ConfirmTransferAsync");instance.Refresh();Check(api.Transfers==1&&item.QualityWarehouseId==2&&!Html().Contains(item.SerialNumber),"confirmed transfer moves visible stock out of its source tab");
            var selection=(HashSet<Guid>)instance.Get("_selected")!;foreach(var row in items)selection.Add(row.HandpanId);await instance.Call("BeginSelectedTransfer");var selected=(List<WarehouseHandpanDto>)instance.Get("_transferItems")!;Check(selected.All(x=>x.QualityWarehouseId==1),"bulk transfer uses visible tier stock, excluding hidden selections");await instance.Call("CancelTransfer");await instance.Call("SelectWarehouse",3);Check(((HashSet<Guid>)instance.Get("_selected")!).Count==0,"changing tabs clears selections");
        }
        if(role=="Administrator")
        {
            var qc=await renderer.RenderComponentAsync<QcPreview>(ParameterView.Empty);var qi=QcPreview.Instance!;string QHtml()=>WebUtility.HtmlDecode(qc.ToHtmlString());Check(QHtml().Contains("انبار مقصد را انتخاب کنید")&&QHtml().Contains("Economy")&&QHtml().Contains("Signature"),"QC shows the three warehouse destination choices");if(preview is not null)await File.WriteAllTextAsync(Path.Combine(preview,"qc-warehouse.html"),qc.ToHtmlString());await qi.Call("ApproveQualityControlAsync");Check(api.QcRequests==0,"QC approval cannot submit before choosing a destination");qi.Set("_qualityWarehouseId",3);await qi.Call("ApproveQualityControlAsync");Check(api.QcRequests==1&&api.LastQualityWarehouse==3,"QC approval submits the selected warehouse");
            qi.Set("_successMessage",null);qi.Set("_bowl",new DimpleBowlDto{ProductionCode="QC-FIXTURE",Stage=14,BowlType=1});await qi.Call("SelectQcExport");qi.Refresh();await qi.Call("ApproveQualityControlAsync");Check(api.QcRequests==1,"export QC requires a country before submitting");
            Check(QHtml().Contains("ایران")&&QHtml().Contains("لندن")&&QHtml().Contains("ترکیه")&&QHtml().Contains("آلمان"),"export selection reveals four destinations");
            if(preview is not null)await File.WriteAllTextAsync(Path.Combine(preview,"qc-export.html"),qc.ToHtmlString());
            qi.Set("_exportWarehouseLocation",2);await qi.Call("ApproveQualityControlAsync");Check(api.LastExportLocation==2&&api.QcRequests==2,"QC persists selected export country");
            ((List<MaterialDto>)qi.Get("_packagingMaterials")!).AddRange(new List<MaterialDto>{new(){Id=Guid.NewGuid(),Name="روغن",Quantity=20},new(){Id=Guid.NewGuid(),Name="دستمال",Quantity=20},new(){Id=Guid.NewGuid(),Name="پایه",Quantity=20},new(){Id=Guid.NewGuid(),Name="پنگارد",Quantity=20},new(){Id=Guid.NewGuid(),Name="کارتن",Quantity=20},new(){Id=Guid.NewGuid(),Name="مثلثی",Quantity=20},new(){Id=Guid.NewGuid(),Name="سافت کیس",Quantity=20},new(){Id=Guid.NewGuid(),Name="هارد کیس",Quantity=20}});
            await qi.Call("SelectDefaultPackagingMaterials");Check(((HashSet<Guid>)qi.Get("_selectedPackagingMaterialIds")!).Count==7,"packaging defaults select six accessories plus soft case");
        }
    });
}
Console.WriteLine("Warehouse tier API and UI smoke passed.");
static void Check(bool value,string message){if(!value)throw new Exception(message);Console.WriteLine("PASS "+message);}
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
        if(path.EndsWith("/instrument-warehouses"))data=warehouses;
        else if(path.Contains("/instrument-warehouses/")&&request.Method==HttpMethod.Put){Renames++;var d=await request.Content!.ReadFromJsonAsync<JsonElement>(ct);var id=int.Parse(path.Split('/').Last());var item=warehouses.Single(x=>x.Id==id);item.Name=d.GetProperty("name").GetString()!;item.Version=Guid.NewGuid();data=item;}
        else if(path.EndsWith("/instrument-warehouses/transfer")){Transfers++;var d=await request.Content!.ReadFromJsonAsync<JsonElement>(ct);var id=d.GetProperty("warehouseId").GetInt32();var rows=d.GetProperty("items").EnumerateArray().Select(x=>Stock.Single(s=>s.HandpanId==x.GetProperty("handpanId").GetGuid())).ToList();foreach(var row in rows){row.QualityWarehouseId=id;row.Version=Guid.NewGuid();}data=rows.Select(x=>new WarehouseMoveResult{HandpanId=x.HandpanId,QualityWarehouseId=x.QualityWarehouseId,Version=x.Version}).ToList();}
        else if(path.EndsWith("/production/warehouse"))data=Stock;
        else if(path.EndsWith("/qc/complete")){QcRequests++;var d=await request.Content!.ReadFromJsonAsync<JsonElement>(ct);LastQualityWarehouse=d.GetProperty("qualityWarehouseId").ValueKind==JsonValueKind.Null?1:d.GetProperty("qualityWarehouseId").GetInt32();LastExportLocation=d.GetProperty("exportWarehouseLocation").ValueKind==JsonValueKind.Null?null:d.GetProperty("exportWarehouseLocation").GetInt32();data=new DimpleBowlDto{ProductionCode="QC-FIXTURE",Stage=16,BowlType=1};}
        else if(path.EndsWith("/sales-attribution/options"))data=new SalesAttributionOptionsDto();
        return new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(data)};
    }
}
