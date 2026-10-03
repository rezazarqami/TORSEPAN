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
        services.AddScoped(_ => new HttpClient(new FixtureApi()) { BaseAddress = new Uri("https://fixture.invalid/api/") });
        services.AddScoped<ApiClient>(); services.AddScoped<OrderService>(); services.AddScoped<ScaleService>();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        await using var renderer = new HtmlRenderer(scope.ServiceProvider, provider.GetRequiredService<ILoggerFactory>());
        foreach (var (type, name) in new[] { (typeof(Orders), "new-order.html"), (typeof(OrderListPreview), "order-list.html"), (typeof(OrderCodePreview), "assign-code.html") })
        {
            var markup = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync(type, ParameterView.Empty)).ToHtmlString());
            if (!markup.Contains("ثبت سفارش") || !markup.Contains("orders-hero")) throw new Exception("Orders preview failed");
            var styles = await File.ReadAllTextAsync(Path.Combine(root, $"TORSEPAN.Panel/obj/{buildConfiguration}/net10.0/scopedcss/Components/Pages/Orders.razor.rz.scp.css"));
            var global = await File.ReadAllTextAsync(Path.Combine(root, "TORSEPAN.Panel/wwwroot/app.css"));
            var bootstrapPath = Path.Combine(root, "TORSEPAN.Panel/wwwroot/lib/bootstrap/dist/css/bootstrap.min.css");
            var bootstrap = File.Exists(bootstrapPath) ? await File.ReadAllTextAsync(bootstrapPath) : ".form-control,.form-select{display:block;width:100%;padding:10px 14px;border:1px solid #d9e3e8;background:#fff;font:inherit}.btn{border:0;cursor:pointer;font:inherit}.btn:disabled{opacity:.6}.visually-hidden{position:absolute;width:1px;height:1px;overflow:hidden}";
            var html = "<!doctype html><html lang='fa' dir='rtl'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>پیش‌نمایش سفارش‌ها</title><style>" + bootstrap + global + styles + "@font-face{font-family:Vazirmatn;src:url('Vazirmatn-Regular.woff2')}body{font-family:Vazirmatn,sans-serif;margin:0;background:#f4f7f9}main{max-width:1120px;padding:25px;margin:auto}*{box-sizing:border-box}</style></head><body><main>" + markup + "</main></body></html>";
            await File.WriteAllTextAsync(Path.Combine(output, name), html);
            Console.WriteLine("PASS actual Razor component renders " + name);
        }
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
    private sealed class FixtureApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("scales"))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new[] { new { Id = Guid.NewGuid(), Name = "D Kurd Custom", Usage = 32 }, new { Id = Guid.NewGuid(), Name = "F Pygmy", Usage = 32 } }) });
            var now = DateTime.UtcNow;
            OrderDto Make(string name, string scale, int days, int ago, string? code, string stage, string[] completed) => new(Guid.NewGuid(), name, Guid.NewGuid(), scale, days, now.AddDays(-ago), now.AddDays(days - ago), code, stage, "در انتظار", completed,
                Enumerable.Range(1, 4).Select(n => new OrderReminderDto(n, now.AddDays(-ago + days * n / 4d), n <= 2 ? now.AddDays(-1) : null, false)).ToArray());
            var orders = new[] { Make("آقای رضایی", "D Kurd Custom", 30, 24, "TP-1042", "در انتظار فاین‌تیون", ["دیمپل کاسه رو", "شیپ کاسه رو", "پخت کاسه رو", "تیون"]), Make("خانم موسوی", "F Pygmy", 60, 8, null, "در انتظار ثبت کد ساز", []), Make("آقای کریمی", "E Sabye", 15, 17, "TP-1018", "در انتظار کنترل کیفیت", ["تیون", "چسب", "فاین‌تیون"]) };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(orders) });
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
