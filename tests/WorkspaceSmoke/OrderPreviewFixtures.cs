using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using TORSEPAN.Application.Orders;
using TORSEPAN.Panel.Components.Pages;
using TORSEPAN.Panel.Services;
using TORSEPAN.Panel.Services.Api;
using TORSEPAN.Panel.Services.Auth;

internal static class OrderPreviewFixtures
{
    public static async Task RenderAsync()
    {
        var output = Environment.GetEnvironmentVariable("TORSEPAN_ORDER_PREVIEW_DIR");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        var root = FindRoot();
        var buildConfiguration = typeof(Orders).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? "Release";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IJSRuntime, FixtureJs>();
        services.AddScoped<TokenStorage>();
        var fixtureApi = new FixtureApi();
        services.AddScoped(_ => new HttpClient(fixtureApi) { BaseAddress = new Uri("https://fixture.invalid/api/") });
        services.AddScoped<ApiClient>(); services.AddScoped<OrderService>(); services.AddScoped<ScaleService>(); services.AddScoped<DesignService>();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        await using var renderer = new HtmlRenderer(scope.ServiceProvider, provider.GetRequiredService<ILoggerFactory>());
        foreach (var (type, name) in new[] { (typeof(Orders), "new-order.html"), (typeof(SimplePreview), "simple-order.html"), (typeof(IncompletePreview), "incomplete-order.html"), (typeof(EditPreview), "edit-order.html"), (typeof(CompositionPreview), "composition.html"), (typeof(DraftPreview), "drafts.html"), (typeof(OrderListPreview), "order-list.html"), (typeof(OverviewPreview), "overview-50.html"), (typeof(OrderCodePreview), "assign-code.html") })
        {
            fixtureApi.EmptyDesigns = type == typeof(SimplePreview);
            var markup = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync(type, ParameterView.Empty)).ToHtmlString());
            if (!WebUtility.HtmlDecode(markup).Contains("ساخت سفارش") || !markup.Contains("orders-hero")) throw new Exception("Orders preview failed");
            var styles = await File.ReadAllTextAsync(Path.Combine(root, $"TORSEPAN.Panel/obj/{buildConfiguration}/net10.0/scopedcss/projectbundle/TORSEPAN.Panel.bundle.scp.css"));
            var global = await File.ReadAllTextAsync(Path.Combine(root, "TORSEPAN.Panel/wwwroot/app.css"));
            var bootstrapPath = Path.Combine(root, "TORSEPAN.Panel/wwwroot/lib/bootstrap/dist/css/bootstrap.min.css");
            var bootstrap = File.Exists(bootstrapPath) ? await File.ReadAllTextAsync(bootstrapPath) : ".form-control,.form-select{display:block;width:100%;padding:10px 14px;border:1px solid #d9e3e8;background:#fff;font:inherit}.btn{border:0;cursor:pointer;font:inherit}.btn:disabled{opacity:.6}.visually-hidden{position:absolute;width:1px;height:1px;overflow:hidden}";
            var html = "<!doctype html><html lang='fa' dir='rtl'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>پیش‌نمایش سفارش‌ها</title><style>" + bootstrap + global + styles + "@font-face{font-family:Vazirmatn;src:url('Vazirmatn-Regular.woff2')}body{font-family:Vazirmatn,sans-serif;margin:0;background:#f4f7f9}main{max-width:1120px;padding:25px;margin:auto}*{box-sizing:border-box}</style></head><body><main>" + markup + "</main></body></html>";
            await File.WriteAllTextAsync(Path.Combine(output, name), html);
            Console.WriteLine("PASS actual Razor component renders " + name);
            if(type==typeof(EditPreview))
            {
                await renderer.Dispatcher.InvokeAsync(async()=>await (Task)typeof(Orders).GetMethod("SaveDraftAsync",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(EditPreview.Instance,[true])!);
                if(fixtureApi.LastUpdate is null || fixtureApi.LastUpdate.Lines.Any(x=>!x.LineId.HasValue))
                    throw new Exception("Final edit must PUT stable line IDs to order API");
                Console.WriteLine("PASS final order editor sends stable line IDs to update endpoint");
            }
        }
        Directory.CreateDirectory(Path.Combine(output,"images/brand"));
        File.Copy(Path.Combine(root,"TORSEPAN.Panel/wwwroot/images/brand/torsepan-mark-new.webp"),Path.Combine(output,"images/brand/torsepan-mark-new.webp"),true);
        File.Copy(Path.Combine(root, "TORSEPAN.Panel/wwwroot/fonts/Vazirmatn-Regular.woff2"), Path.Combine(output, "Vazirmatn-Regular.woff2"), true);
    }

    private static string FindRoot()
    {
        for (var d = new DirectoryInfo(Directory.GetCurrentDirectory()); d != null; d = d.Parent)
            if (Directory.Exists(Path.Combine(d.FullName, "TORSEPAN.Panel"))) return d.FullName;
        throw new Exception("Repository root not found");
    }
    private sealed class FixtureJs : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => ValueTask.FromResult(default(TValue)!);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken ct, object?[]? args) => ValueTask.FromResult(default(TValue)!);
    }
    private static readonly Guid _plain=Guid.NewGuid(), _acid=Guid.NewGuid(), _mill=Guid.NewGuid(), _scale9=Guid.NewGuid(), _scale12=Guid.NewGuid(), _standard=Guid.NewGuid(), _both=Guid.NewGuid();
    private sealed class SimplePreview : Orders
    {
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(Orders).GetField("_customerName",flags)!.SetValue(this,"مشتری دیزاین ساده");
            typeof(Orders).GetField("_duration",flags)!.SetValue(this,"۳۰");
            var list=(System.Collections.IList)typeof(Orders).GetField("_items",flags)!.GetValue(this)!;
            list[0]!.GetType().GetProperty("ScaleId")!.SetValue(list[0],_standard);
            if (!(bool)typeof(Orders).GetProperty("CanSave",flags)!.GetValue(this)!)
                throw new Exception("Simple order with standard instrument scale cannot be saved with an empty design catalog");
        }
    }
    private sealed class IncompletePreview : Orders
    {
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(Orders).GetField("_customerName",flags)!.SetValue(this,"مشتری ترکیب ناقص");
            typeof(Orders).GetField("_duration",flags)!.SetValue(this,"۳۰");
            var list=(System.Collections.IList)typeof(Orders).GetField("_items",flags)!.GetValue(this)!;
            list[0]!.GetType().GetProperty("DesignId")!.SetValue(list[0],_acid);
        }
    }
    private sealed class EditPreview : Orders
    {
        public static EditPreview? Instance;
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            Instance=this;
            var order=((List<OrderDto>)typeof(Orders).GetField("_orders",flags)!.GetValue(this)!)[0];
            await (Task)typeof(Orders).GetMethod("EditDraftAsync",flags)!.Invoke(this,[order])!;
            if(!(bool)typeof(Orders).GetProperty("CanSave",flags)!.GetValue(this)!)throw new Exception("Final order edit is not saveable");
        }
    }
    private sealed class CompositionPreview : Orders
    {
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(Orders).GetField("_customerName",flags)!.SetValue(this,"آقای رضایی");
            typeof(Orders).GetField("_duration",flags)!.SetValue(this,"۳۰");
            var list=(System.Collections.IList)typeof(Orders).GetField("_items",flags)!.GetValue(this)!;
            var type=list[0]!.GetType();list.Clear();
            foreach(var (design,scale) in new[]{(_plain,_scale9),(_acid,_scale12),(_mill,_scale9)})
            {var line=Activator.CreateInstance(type,true)!;type.GetProperty("ScaleId")!.SetValue(line,scale);type.GetProperty("DesignId")!.SetValue(line,design);type.GetProperty("Quantity")!.SetValue(line,"۵");list.Add(line);}
        }
    }
    private sealed class DraftPreview : Orders
    {
        protected override async Task OnInitializedAsync(){await base.OnInitializedAsync();typeof(Orders).GetField("_tab",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(this,"drafts");}
    }
    private sealed class FixtureApi : HttpMessageHandler
    {
        public bool EmptyDesigns { get; set; }
        public SaveOrderDraftRequest? LastUpdate;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if(request.Method==HttpMethod.Put && request.RequestUri!.AbsolutePath.Contains("/orders/"))
            {
                LastUpdate=await request.Content!.ReadFromJsonAsync<SaveOrderDraftRequest>(cancellationToken:ct);
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }
            if (request.RequestUri!.AbsolutePath.EndsWith("designs/types"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(EmptyDesigns ? [] : new[] { new { Id = _plain, Name = "ساده" }, new { Id = _acid, Name = "اسیدکاری" }, new { Id = _mill, Name = "فرزکاری" } }) };
            if (request.RequestUri!.AbsolutePath.EndsWith("scales"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new[] { new { Id = _scale9, Name = "D Kurd Custom 9", Usage = 32 }, new { Id = _scale12, Name = "F Pygmy 12", Usage = 32 },
                    new { Id = _standard, Name = "E Sabye standard", Usage = 4 },
                    new { Id = _both, Name = "Both instrument catalogs", Usage = 36 },
                    new { Id = Guid.NewGuid(), Name = "Bowl top only", Usage = 1 },
                    new { Id = Guid.NewGuid(), Name = "Bowl bottom only", Usage = 2 },
                    new { Id = Guid.NewGuid(), Name = "Custom bowl only", Usage = 24 } }) };
            var now = DateTime.UtcNow;
            OrderDto Make(string name, string scale, int days, int ago, string? code, string stage, string[] completed) => new(Guid.NewGuid(), name, Guid.NewGuid(), scale, days, now.AddDays(-ago), now.AddDays(days - ago), code, stage, "در انتظار", completed,
                Enumerable.Range(1, 4).Select(n => new OrderReminderDto(n, now.AddDays(-ago + days * n / 4d), n <= 2 ? now.AddDays(-1) : null, false)).ToArray());
            var orders = new[] { Make("آقای رضایی", "D Kurd Custom", 30, 24, "TP-1042", "در انتظار فاین‌تیون", ["دیمپل کاسه رو", "شیپ کاسه رو", "پخت کاسه رو", "تیون"]), Make("خانم موسوی", "F Pygmy", 60, 8, null, "در انتظار ثبت کد ساز", []), Make("آقای کریمی", "E Sabye", 15, 17, "TP-1018", "در انتظار کنترل کیفیت", ["تیون", "چسب", "فاین‌تیون"]) };
            var lines = new[] {
                new OrderLineDto(Guid.NewGuid(),1,_scale9,"D Kurd Custom 9",_plain,"ساده",5,[new(1,"TP-1042","در انتظار فاین‌تیون","در انتظار",["دیمپل کاسه رو","شیپ کاسه رو","تیون","چسب"])]),
                new OrderLineDto(Guid.NewGuid(),2,_scale12,"D Kurd Custom 12",_acid,"اسیدکاری",5,[]),
                new OrderLineDto(Guid.NewGuid(),3,_scale9,"F Pygmy 9",_mill,"فرزکاری",5,[])
            };
            orders[0] = orders[0] with { Lines = lines };
            orders[1] = orders[1] with { IsDraft = true, Lines = lines.Select(x=>x with {Id=Guid.NewGuid(),Instruments=[]}).ToArray() };
            orders[2] = orders[2] with { Lines = [lines[0] with {Id=Guid.NewGuid(),Quantity=30,Instruments=[new(1,"TP-1018","در انتظار کنترل کیفیت","در انتظار",["تیون","چسب","فاین‌تیون"])]}] };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(orders) };
        }
    }
    private class OrderListPreview : Orders
    {
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            typeof(Orders).GetField("_tab", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(this, "list");
        }
    }
    private sealed class OverviewPreview : OrderListPreview
    {
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var orders=(List<OrderDto>)typeof(Orders).GetField("_orders",flags)!.GetValue(this)!;
            var stages=new[]{"در انتظار دیمپل","در انتظار شیپ","در انتظار تیون","در انتظار فاین‌تیون","در اتاق چسب"};
            orders[0]=orders[0] with {Lines=[orders[0].Lines[0] with {Quantity=50,Instruments=Enumerable.Range(1,50)
                .Select(n=>new OrderInstrumentDto(n,$"TP-{n:0000}",stages[(n-1)%stages.Length],"در انتظار",[])).ToArray()}]};
        }
    }
    private sealed class OrderCodePreview : OrderListPreview
    {
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            var orders = (List<OrderDto>)typeof(Orders).GetField("_orders", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(this)!;
            typeof(Orders).GetMethod("OpenCode", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(this, new object[] { orders[0] });
        }
    }
}
