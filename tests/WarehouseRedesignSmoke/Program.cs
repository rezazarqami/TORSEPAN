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

var fixture=new InventoryFixture();var services=new ServiceCollection();services.AddLogging();services.AddAuthorizationCore();services.AddCascadingAuthenticationState();services.AddSingleton<AuthenticationStateProvider>(new AuthFixture());services.AddSingleton<IJSRuntime,JsFixture>();services.AddSingleton<NavigationManager,NavFixture>();services.AddSingleton(new HttpClient(fixture){BaseAddress=new Uri("https://fixture.invalid/api/")});services.AddScoped<TokenStorage>();services.AddScoped<ApiClient>();services.AddScoped<ProductionService>();services.AddScoped<BowlService>();services.AddScoped<AccountingService>();services.AddScoped<SalesAttributionService>();services.AddScoped<HandpanPhotoService>();
await using var provider=services.BuildServiceProvider();await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
var output=Environment.GetEnvironmentVariable("WAREHOUSE_PREVIEW_DIR")??"/tmp/warehouse-redesign-preview";Directory.CreateDirectory(output);
await renderer.Dispatcher.InvokeAsync(async()=>{
 var result=await renderer.RenderComponentAsync<InventoryPreview>();var page=InventoryPreview.Current!;
 string Html()=>WebUtility.HtmlDecode(result.ToHtmlString());async Task Snapshot(string name){page.Refresh();await File.WriteAllTextAsync(Path.Combine(output,name+".html"),result.ToHtmlString());}
 void Check(bool condition,string message){if(!condition)throw new Exception(message);Console.WriteLine("PASS "+message);}
 Check(!Html().Contains("warehouse-search")&&!Html().Contains("warehouse-filter-toggle")&&!Html().Contains("gallery-open"),"obsolete search, filters and separate gallery controls removed");
 Check(fixture.SaleReads==0,"inventory entry does not preload sales-only options");
 await Snapshot("list");
 foreach(var count in new[]{9,10,11,12}){await page.Call("SelectNotesAsync",count);page.Refresh();Check(page.Visible().All(x=>x.NoteCount==count)&&page.Visible().Count>0,"typed "+count+" note filter");await page.Call("SelectNotesAsync",count);}
 Check(page.Visible().Count==6,"note tabs never hide unknown counts until explicitly filtered");
 await page.Call("SetViewAsync",true);page.Refresh();Check(Html().Contains("stock-grid")&&!Html().Contains("inventory-gallery-overlay"),"gallery stays in the inventory board");Check(Html().Contains("بدون عکس"),"instruments without photos remain in gallery");await Snapshot("gallery");
 await page.Call("SelectNotesAsync",9);page.Refresh();Check(fixture.LastPhotoIds.SetEquals(page.Visible().Select(x=>x.HandpanId)),"gallery requests only matching instruments");await page.Call("SelectNotesAsync",9);await page.Call("SetViewAsync",false);
 var first=page.Visible()[0];await page.Call("ToggleSelected",first.HandpanId,true);page.Refresh();Check(Html().LastIndexOf("warehouse-selection-bar")>Html().IndexOf("stock-item"),"group actions follow inventory rows");
 await page.Call("ToggleAsync",first);page.Refresh();await Snapshot("expanded");Check(Html().Contains("فؤاد احمدفیض")&&Html().Contains("روغن"),"expanded detail uses actual operation and packaging data");Check(Html().Contains("فول پولیش")&&Html().Contains("(ساده)")&&Html().Contains("وضعیت")&&!Html().Contains("<small>کاسه رو</small>"),"status and both designs replace bowl and warehouse facts");await Snapshot("expanded");
 await page.Call("BeginSelectedTransfer");page.Refresh();Check(Html().Contains("انتقال ساز"),"bulk transfer workflow remains reachable");await page.Call("CancelTransfer");
 await page.Call("BeginSale",first);page.Refresh();Check(fixture.SaleReads==2&&Html().Contains("sale-title"),"sales options load on demand and sale dialog remains reachable");await page.Call("Cancel");
 await page.Call("SelectWarehouse",2);Check(page.Visible().All(x=>x.QualityWarehouseId==2),"warehouse switching scopes inventory");
});
await renderer.Dispatcher.InvokeAsync(async()=>{
 var selectedSale=new List<Guid>();var selectedMove=new List<Guid>();Guid? deleted=null;
 var result=await renderer.RenderComponentAsync<InventoryPreview>(ParameterView.FromDictionary(new Dictionary<string,object?>{
 [nameof(WarehouseList.ExportItems)]=fixture.Stock.Take(2).ToList(),[nameof(WarehouseList.ExportWarehouseName)]="انبار ترکیه",
 [nameof(WarehouseList.ExportSale)]=EventCallback.Factory.Create<IReadOnlyList<Guid>>(fixture,ids=>selectedSale=ids.ToList()),
 [nameof(WarehouseList.ExportTransfer)]=EventCallback.Factory.Create<IReadOnlyList<Guid>>(fixture,ids=>selectedMove=ids.ToList()),
 [nameof(WarehouseList.ExportDeleted)]=EventCallback.Factory.Create<Guid>(fixture,id=>deleted=id)}));
 var page=InventoryPreview.Current!;var first=page.Visible()[0];
 if(page.Visible().Count!=2||!result.ToHtmlString().Contains("warehouse-note-tabs"))throw new Exception("export board must retain all supplied instruments and shared note tabs");
 await page.Call("ToggleSelected",first.HandpanId,true);await page.Call("BeginSelectedTransfer");await page.Call("BeginSelectedSale");
 if(!selectedSale.SequenceEqual(new[]{first.HandpanId})||!selectedMove.SequenceEqual(new[]{first.HandpanId}))throw new Exception("export bulk actions lost selected IDs");
 await page.Call("DeleteAsync",first);if(deleted!=first.HandpanId||!fixture.DeletedIds.Contains(first.HandpanId))throw new Exception("export delete must use handpan endpoint and notify host");
 Console.WriteLine("PASS shared export note tabs, bulk callbacks and confirmed handpan deletion");
 var parent=await renderer.RenderComponentAsync<ExportPreview>();var host=ExportPreview.Current!;
 for(var country=1;country<=7;country++){await host.Call("SelectWarehouseAsync",country);host.Refresh();var html=WebUtility.HtmlDecode(parent.ToHtmlString());if(!html.Contains("warehouse-redesign"))throw new Exception("missing shared board for destination "+country);if(html.LastIndexOf("compact-inventory-tabs")<html.IndexOf("stock-item")||html.LastIndexOf("compact-inventory-tabs")>html.LastIndexOf("warehouse-selection-bar"))throw new Exception("inventory kind buttons must follow items and precede bulk actions");}
 await File.WriteAllTextAsync(Path.Combine(output,"export.html"),parent.ToHtmlString());Console.WriteLine("PASS all seven export destinations use shared board and bottom type controls");
});
Console.WriteLine("Warehouse redesign smoke passed; preview "+output);

public sealed class InventoryPreview:WarehouseList{
 public static InventoryPreview? Current;protected override async Task OnInitializedAsync(){await base.OnInitializedAsync();Current=this;}
 public async Task Call(string name,params object?[] args){var value=typeof(WarehouseList).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(this,args);if(value is Task task)await task;}
 public List<WarehouseHandpanDto> Visible()=>((IEnumerable<WarehouseHandpanDto>)typeof(WarehouseList).GetProperty("Items",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(this)!).ToList();
 public void Refresh()=>StateHasChanged();
}
public sealed class InventoryFixture:HttpMessageHandler{
 public HashSet<Guid> DeletedIds=[];public int SaleReads;public HashSet<Guid> LastPhotoIds=[];
 public List<WarehouseHandpanDto> Stock=[new(){HandpanId=Guid.NewGuid(),SerialNumber="00018",TopBowlCode="18",MaterialName="Stainless Steel",ScaleName="D Kurd 9",NoteCount=9},new(){HandpanId=Guid.NewGuid(),SerialNumber="07127",TopBowlCode="7127",MaterialName="VELVET",ScaleName="Dkurd- Cyclops 12",NoteCount=12},new(){HandpanId=Guid.NewGuid(),SerialNumber="VS-00009",TopBowlCode="VS-00009",MaterialName="VELVET",ScaleName="D Kurd 11",NoteCount=11},new(){HandpanId=Guid.NewGuid(),SerialNumber="SS-00073",TopBowlCode="SS-00073",MaterialName="Stainless Steel",ScaleName="D Kurd 11",NoteCount=11},new(){HandpanId=Guid.NewGuid(),SerialNumber="SS-00071",TopBowlCode="SS-00071",MaterialName="Stainless Steel",ScaleName="D Kurd 9",NoteCount=9},new(){HandpanId=Guid.NewGuid(),SerialNumber="SS-00080",TopBowlCode="SS-00080",MaterialName="VELVET",ScaleName="C Aegean 10",NoteCount=10},new(){HandpanId=Guid.NewGuid(),SerialNumber="ST-00004",TopBowlCode="ST-00004",MaterialName="Stainless Steel",ScaleName="D Kurd 13",NoteCount=13,QualityWarehouseId=2}];
 public InventoryFixture(){for(int i=0;i<Stock.Count;i++){Stock[i].Version=Guid.NewGuid();Stock[i].BottomBowlCode="SB-"+(30+i);Stock[i].WarehouseEntryDate=new DateTime(2026,11,3,20,13,0).AddDays(-i);}}
 private static readonly Guid PhotoId=Guid.Parse("aaaaaaaa-1111-4111-8111-111111111111");
 private static byte[] Image()=>File.ReadAllBytes("TORSEPAN.Panel/wwwroot/images/brand/handpan-light.webp");
 protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct){var p=request.RequestUri!.AbsolutePath;object data=Array.Empty<object>();
 if(request.Method==HttpMethod.Delete){DeletedIds.Add(Guid.Parse(p.Split('/').Last()));data=new{};}
 else if(p.EndsWith("/export-warehouse"))data=Stock.Select((x,i)=>new ExportWarehouseItemDto{Id=x.HandpanId,ItemType="handpan",ItemKind="ساز",ProductionCode=x.SerialNumber,Detail=x.ScaleName,Location=i+1,Inventory=x}).ToArray();
 else if(p.EndsWith("/production/warehouse"))data=Stock;
 else if(p.EndsWith("/instrument-warehouses"))data=new[]{new InstrumentWarehouseDto{Id=1,Name="Economy",Version=Guid.NewGuid()},new InstrumentWarehouseDto{Id=2,Name="Studio",Version=Guid.NewGuid()},new InstrumentWarehouseDto{Id=3,Name="Signature",Version=Guid.NewGuid()}};
 else if(p.EndsWith("/details")){var item=Stock.Single(x=>p.Contains(x.HandpanId.ToString()));item.DesignName="فول پولیش";item.BottomDesignName="ساده";item.PackagingItems=["روغن","دستمال","پنگارد","سافت کیس"];item.Operations=new[]{("دیمپل","فؤاد احمدفیض"),("شیپ","شاهین نجفی"),("تیون","علیرضا ضرغامی"),("فاین تیون","علیرضا ضرغامی"),("QC","آبتین ترسه"),("بسته‌بندی","جاوید احمدفیض")}.Select((x,i)=>new WarehouseOperationDto{Operation=x.Item1,PerformedBy=x.Item2,PerformedAt=new DateTime(2026,8,12,20,2,0).AddMinutes(7*i)}).ToList();data=item;}
 else if(p.EndsWith("/warehouse/gallery")){var input=await request.Content!.ReadFromJsonAsync<JsonElement>(ct);LastPhotoIds=input.GetProperty("handpanIds").EnumerateArray().Select(x=>x.GetGuid()).ToHashSet();data=Stock.Where((x,i)=>i!=2&&LastPhotoIds.Contains(x.HandpanId)).Select(x=>new WarehouseGalleryPhotoDto{HandpanId=x.HandpanId,Id=PhotoId,Thumbnail=Image()}).ToArray();}
 else if(p.EndsWith("/photos"))data=new[]{new HandpanPhotoDto{Id=PhotoId,CreatedAt=DateTime.UtcNow}};
 else if(p.Contains("/photos/"))return new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(Image())};
 else if(p.EndsWith("/accounting/parties")){SaleReads++;data=Array.Empty<AccountingPartyDto>();}
 else if(p.EndsWith("/sales-attribution/options")){SaleReads++;data=new SalesAttributionOptionsDto();}
 return new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(data)};}
}
public sealed class AuthFixture:AuthenticationStateProvider{public override Task<AuthenticationState> GetAuthenticationStateAsync()=>Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,Guid.NewGuid().ToString()),new Claim(ClaimTypes.Name,"مدیر"),new Claim(ClaimTypes.Role,"Administrator")},"test"))));}
public sealed class JsFixture:IJSRuntime{public ValueTask<TValue> InvokeAsync<TValue>(string identifier,object?[]? args)=>ValueTask.FromResult(typeof(TValue)==typeof(bool)?(TValue)(object)true:default(TValue)!);public ValueTask<TValue> InvokeAsync<TValue>(string identifier,CancellationToken cancellationToken,object?[]? args)=>InvokeAsync<TValue>(identifier,args);}
public sealed class NavFixture:NavigationManager{public NavFixture(){Initialize("https://fixture.invalid/","https://fixture.invalid/warehouse/list");}protected override void NavigateToCore(string uri,bool forceLoad){}}

public sealed class ExportPreview:ExportWarehouse{
 public static ExportPreview? Current;protected override async Task OnInitializedAsync(){await base.OnInitializedAsync();Current=this;}
 public async Task Call(string name,params object?[] args){var value=typeof(ExportWarehouse).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(this,args);if(value is Task task)await task;}
 public void Refresh()=>StateHasChanged();
}
