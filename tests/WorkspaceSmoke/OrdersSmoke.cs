using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TORSEPAN.API.Controllers;
using TORSEPAN.Application.Orders;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Infrastructure.Services;

internal static class OrdersSmoke
{
    private static void Check(bool result, string message)
    { if (!result) throw new Exception(message); Console.WriteLine("PASS " + message); }
    private static async Task Reject(Func<Task> action, string message)
    { try { await action(); } catch (OrderValidationException) { Check(true, message); return; } throw new Exception(message); }

    public static async Task RunAsync()
    {
        if (OperatingSystem.IsLinux()) NativeLibrary.SetDllImportResolver(typeof(SQLitePCL.SQLite3Provider_sqlite3).Assembly,
            (name, assembly, path) => name == "sqlite3" ? NativeLibrary.Load("libsqlite3.so.0") : IntPtr.Zero);
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TORSEPANDbContext>().UseSqlite(connection).Options;
        await using var db = new TORSEPANDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var user = new User("order-smoke", "مدیر آزمایشی");
        var scale = new Scale("D Kurd Custom", ScaleUsage.CustomHandpan);
        var standard = new Scale("Standard", ScaleUsage.Handpan);
        var customTop = new Scale("Top only", ScaleUsage.CustomTopBowl);
        var inactive = new Scale("Inactive", ScaleUsage.CustomHandpan); inactive.Deactivate();
        db.AddRange(user, scale, standard, customTop, inactive);
        await db.SaveChangesAsync();
        var clock = new TestClock(new DateTimeOffset(2026, 10, 3, 6, 0, 0, TimeSpan.Zero));
        var orders = new CustomerOrderService(db, clock);
        var pageAccess = typeof(TORSEPAN.Panel.Components.Pages.Orders).GetCustomAttribute<AuthorizeAttribute>();
        var apiAccess = typeof(OrdersController).GetCustomAttribute<AuthorizeAttribute>();
        Check(pageAccess is not null && pageAccess.Roles is null && apiAccess is not null && apiAccess.Roles is null,
            "panel and API allow every authenticated user and still require sign-in");
        await Reject(() => orders.CreateAsync(new(" ", scale.Id, 30), user.Id, default), "empty customer is rejected");
        await Reject(() => orders.CreateAsync(new("نام", scale.Id, 0), user.Id, default), "zero duration is rejected");
        await Reject(() => orders.CreateAsync(new("نام", scale.Id, 36501), user.Id, default), "duration overflow is rejected");
        foreach (var badScale in new[] { standard, customTop, inactive })
            await Reject(() => orders.CreateAsync(new("نام", badScale.Id, 30), user.Id, default), "only active custom instrument scales can be ordered: " + badScale.Name);
        var id = await orders.CreateAsync(new("  آقای رضایی  ", scale.Id, 30), user.Id, default);
        var order = await db.CustomerOrders.Include(x => x.Reminders).SingleAsync(x => x.Id == id);
        Check(order.CustomerName == "آقای رضایی" && order.DueAtUtc == clock.GetUtcNow().UtcDateTime.AddDays(30), "order stores customer and exact 30-day deadline");
        Check(order.Reminders.OrderBy(x => x.Milestone).Select(x => (x.DueAtUtc - order.CreatedAtUtc).TotalDays).SequenceEqual(new[] { 7.5, 15, 22.5, 30 }), "four precise milestones include fractional days");
        var oneDay = new CustomerOrder("کوتاه", scale.Id, scale.Name, 1, user.Id, clock.GetUtcNow().UtcDateTime);
        Check(oneDay.Reminders.Select(x => (x.DueAtUtc - oneDay.CreatedAtUtc).TotalHours).SequenceEqual(new[] { 6d, 12, 18, 24 }), "one-day order reminders occur every six hours");
        scale.Rename("Renamed catalog entry");
        await db.SaveChangesAsync();
        Check((await orders.GetAsync(default)).Single().ScaleName == "D Kurd Custom", "existing order preserves its ordered scale after catalog rename");
        await Reject(() => orders.AssignCodeAsync(id, "MISSING", user.Id, default), "unknown instrument code is rejected");
        var material = new Material("Order test material");
        db.Materials.Add(material); await db.SaveChangesAsync();
        var top = new Bowl("ORDER-123", BowlType.Top, true, InstrumentType.Custom, material.Id);
        var bottom = new Bowl("ORDER-BOTTOM", BowlType.Bottom, false, InstrumentType.Custom, material.Id);
        db.AddRange(top, bottom); await db.SaveChangesAsync();
        await Reject(() => orders.AssignCodeAsync(id, bottom.ProductionCode, user.Id, default), "unassembled bottom bowl cannot be assigned as an instrument");
        Check(await orders.AssignCodeAsync(id, "order-۱۲۳", user.Id, default), "code accepts Persian digits and case-insensitive lookup");
        top.ChangeStage(ProductionStage.WaitingForShape);
        db.ProductionEvents.Add(new ProductionEvent(null, null, top.Id, user.Id, ProductionAction.Dimple, EventResult.Completed, null, "completed"));
        db.ProductionEvents.Add(new ProductionEvent(null, null, top.Id, user.Id, ProductionAction.Shape, EventResult.Failed, null, "failed"));
        await db.SaveChangesAsync();
        var early = (await orders.GetAsync(default)).Single();
        Check(early.ProductionStage == "در انتظار شیپ" && early.CompletedOperations.Contains("دیمپل کاسه رو") && !early.CompletedOperations.Contains("شیپ کاسه رو"), "live bowl stage and only successful operations are displayed");
        var second = await orders.CreateAsync(new("خانم موسوی", scale.Id, 60), user.Id, default);
        await Reject(() => orders.AssignCodeAsync(second, top.ProductionCode, user.Id, default), "one instrument cannot be assigned to two orders");
        var assembly = new HandpanAssembly(top.Id, bottom.Id);
        var handpan = assembly.CreateHandpan(top.ProductionCode);
        handpan.ChangeStage(ProductionStage.WaitingForFinalTune);
        handpan.ChangeStatus(ProductionStatus.Waiting);
        db.Add(assembly); db.Add(handpan); await db.SaveChangesAsync();
        var afterAssembly = (await orders.GetAsync(default)).Single(x => x.Id == id);
        Check(afterAssembly.ProductionStage == "در انتظار فاین‌تیون" && order.HandpanId is null, "pre-assembly code automatically follows the resulting handpan");
        Check(!await orders.AssignCodeAsync(Guid.NewGuid(), top.ProductionCode, user.Id, default), "missing order returns not-found");
        Check(OrderTiming.RemainingDays(order.DueAtUtc, order.DueAtUtc.AddDays(2)) == 0 && OrderTiming.Progress(order.CreatedAtUtc, order.DueAtUtc, order.DueAtUtc.AddDays(2)) == 100, "overdue order timing stays bounded");

        var sender = new TestSender();
        var status = new OrderReminderStatus();
        var processor = new OrderReminderProcessor(db, sender, clock, status, Microsoft.Extensions.Logging.Abstractions.NullLogger<OrderReminderProcessor>.Instance);
        await processor.DispatchDueAsync(default);
        Check(sender.Keys.Count == 0, "reminders are not sent before their milestone");
        clock.Now = new DateTimeOffset(order.CreatedAtUtc.AddDays(7.5), TimeSpan.Zero);
        sender.Fail = true;
        await processor.DispatchDueAsync(default);
        var first = order.Reminders.Single(x => x.Milestone == 1);
        Check(first.SentAtUtc is null && first.Attempts == 1 && first.NextAttemptAtUtc == clock.GetUtcNow().UtcDateTime.AddMinutes(5), "failed Telegram delivery stays pending with a five-minute retry");
        sender.Fail = false;
        await processor.DispatchDueAsync(default);
        Check(sender.Keys.Count == 0, "retry backoff prevents a tight send loop");
        clock.Now = clock.Now.AddMinutes(5);
        await processor.DispatchDueAsync(default);
        Check(first.SentAtUtc.HasValue && sender.Keys.Count == 1 && sender.Messages[0].Contains("آقای رضایی") && sender.Messages[0].Contains("زمان باقی‌مانده"), "successful reminder records delivery and includes order timing");
        await processor.DispatchDueAsync(default);
        Check(sender.Keys.Count == 1, "delivered milestone is not sent again");
        clock.Now = new DateTimeOffset(order.DueAtUtc.AddMinutes(1), TimeSpan.Zero);
        await using var restarted = new TORSEPANDbContext(options);
        var restartedProcessor = new OrderReminderProcessor(restarted, sender, clock, status, Microsoft.Extensions.Logging.Abstractions.NullLogger<OrderReminderProcessor>.Instance);
        await restartedProcessor.DispatchDueAsync(default);
        var firstOrderKeys = sender.Keys.Where(x => x.Contains(id.ToString("N"))).ToArray();
        Check(firstOrderKeys.Length == 4 && firstOrderKeys.Distinct().Count() == 4, "restart catches up remaining milestones without repeating earlier success");
        Check(sender.Messages.Any(x => x.Contains("پایان مهلت سفارش") && x.Contains("مهلت این سفارش به پایان رسیده")), "final deadline reminder announces expiration");
        await using (var deleteDb = new TORSEPANDbContext(options))
        {
            var deletion = new CustomerOrderService(deleteDb, clock);
            Check(await deletion.DeleteAsync(id, default), "existing linked order can be deleted");
            Check(!await deleteDb.CustomerOrders.AnyAsync(x => x.Id == id) && !await deleteDb.OrderReminders.AnyAsync(x => x.OrderId == id), "deleting an order removes every reminder");
            Check(await deleteDb.Bowls.CountAsync() == 2 && await deleteDb.Handpans.AnyAsync(x => x.Id == handpan.Id) && await deleteDb.HandpanAssemblies.AnyAsync(x => x.Id == assembly.Id) && await deleteDb.ProductionEvents.AnyAsync(), "deleting an order preserves bowls, assembled instrument and production history");
            Check(!await deletion.DeleteAsync(id, default), "repeated deletion returns not-found");
            Check(await deletion.AssignCodeAsync(second, top.ProductionCode, user.Id, default), "deleted order releases its instrument for another order");
            var sentBefore = sender.Keys.Count(x => x.Contains(id.ToString("N")));
            var afterDeleteProcessor = new OrderReminderProcessor(deleteDb, sender, clock, status, Microsoft.Extensions.Logging.Abstractions.NullLogger<OrderReminderProcessor>.Instance);
            await afterDeleteProcessor.DispatchDueAsync(default);
            Check(sender.Keys.Count(x => x.Contains(id.ToString("N"))) == sentBefore, "deleted order cannot send future reminders");
        }
        await TestAuthorizationAsync(connection, clock, user.Id);
        await TestTelegramTransportAsync();
        await using var postgresModel = new TORSEPANDbContext(new DbContextOptionsBuilder<TORSEPANDbContext>().UseNpgsql("Host=127.0.0.1;Database=unused;Username=unused").Options);
        var script = postgresModel.GetService<IMigrator>().GenerateScript("20260920020000_AddSalesAttribution", "20261003063625_AddCustomerOrders");
        Check(script.Contains("CREATE TABLE \"CustomerOrders\"") && script.Contains("CREATE TABLE \"OrderReminders\"") && !script.Contains("CREATE TABLE \"Accounting") && !script.Contains("DROP INDEX"), "PostgreSQL migration adds only orders and reminders");
        var rollback = postgresModel.GetService<IMigrator>().GenerateScript("20261003063625_AddCustomerOrders", "20260920020000_AddSalesAttribution");
        Check(rollback.Contains("DROP TABLE \"OrderReminders\"") && !rollback.Contains("HandpanPhotos"), "rollback does not touch existing application tables");
        await OrderPreviewFixtures.RenderAsync();
    }

    private static async Task TestAuthorizationAsync(SqliteConnection connection, TestClock clock, Guid userId)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddDbContext<TORSEPANDbContext>(x => x.UseSqlite(connection));
        builder.Services.AddSingleton<TimeProvider>(clock);
        builder.Services.AddScoped<CustomerOrderService>();
        builder.Services.AddSingleton(new TestIdentity(userId));
        builder.Services.AddAuthentication("fixture").AddScheme<AuthenticationSchemeOptions, RoleFixtureHandler>("fixture", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddControllers().AddApplicationPart(typeof(OrdersController).Assembly);
        await using var app = builder.Build();
        app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var http = new HttpClient { BaseAddress = new Uri(address) };
        Check((await http.GetAsync("api/orders")).StatusCode == HttpStatusCode.Unauthorized, "anonymous order API request returns 401");
        Check((await http.PostAsJsonAsync("api/orders", new CreateOrderRequest("نام", Guid.NewGuid(), 30))).StatusCode == HttpStatusCode.Unauthorized, "anonymous order creation returns 401");
        Check((await http.PutAsJsonAsync($"api/orders/{Guid.NewGuid()}/code", new AssignOrderCodeRequest("CODE"))).StatusCode == HttpStatusCode.Unauthorized, "anonymous code assignment returns 401");
        Check((await http.DeleteAsync($"api/orders/{Guid.NewGuid()}")).StatusCode == HttpStatusCode.Unauthorized, "anonymous order deletion returns 401");
        foreach (var role in Enum.GetNames<SystemRole>().Append("AuthenticatedOnly"))
        {
            http.DefaultRequestHeaders.Remove("X-Fixture-Role"); http.DefaultRequestHeaders.Add("X-Fixture-Role", role);
            Check((await http.GetAsync("api/orders")).StatusCode == HttpStatusCode.OK, "order list accessible to authenticated user: " + role);
            var creation = await http.PostAsJsonAsync("api/orders", new CreateOrderRequest(" ", Guid.Empty, 1));
            Check(creation.StatusCode == HttpStatusCode.BadRequest, "authenticated user reaches order creation validation: " + role);
            Check((await http.DeleteAsync($"api/orders/{Guid.NewGuid()}")).StatusCode == HttpStatusCode.NotFound, "authenticated user reaches order deletion: " + role);
            var assignment = await http.PutAsJsonAsync($"api/orders/{Guid.NewGuid()}/code", new AssignOrderCodeRequest("MISSING"));
            Check(assignment.StatusCode == HttpStatusCode.NotFound, "authenticated user reaches code assignment: " + role);
        }
        await app.StopAsync();
    }

    private static async Task TestTelegramTransportAsync()
    {
        var handler = new OrderTransportFixture();
        using var http = new HttpClient(handler);
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Telegram:RelayUrl"] = "https://marketsignalist.ir/torsepan-relay/inventory-alert", ["Telegram:RelaySecret"] = "test-secret"
        }).Build();
        var sender = new TelegramOrderReminderSender(http, configuration);
        await sender.SendAsync("order:0123456789abcdef0123456789abcdef:1", "یادآوری", default);
        Check(handler.Url == "https://marketsignalist.ir/torsepan-relay/order-reminder" && handler.Secret == "test-secret" && handler.Body!.Contains("deliveryKey"), "reminder uses the repaired HTTPS relay, same secret, and durable delivery key");
        handler.Confirmed = false;
        try { await sender.SendAsync("key", "text", default); throw new Exception("unconfirmed delivery was accepted"); }
        catch (InvalidOperationException) { Check(true, "HTTP 200 without confirmed delivery is rejected"); }
    }
    internal sealed class TestClock(DateTimeOffset now) : TimeProvider
    { public DateTimeOffset Now { get; set; } = now; public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class TestSender : IOrderReminderSender
    {
        public bool Fail; public List<string> Keys = []; public List<string> Messages = [];
        public Task SendAsync(string key, string text, CancellationToken ct)
        { if (Fail) throw new HttpRequestException("offline"); Keys.Add(key); Messages.Add(text); return Task.CompletedTask; }
    }
    private sealed record TestIdentity(Guid Id);
    private sealed class RoleFixtureHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, TestIdentity identity)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var role = Request.Headers["X-Fixture-Role"].ToString();
            if (role.Length == 0) return Task.FromResult(AuthenticateResult.NoResult());
            var claims = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, identity.Id.ToString()) }, Scheme.Name);
            if (role != "AuthenticatedOnly") claims.AddClaim(new Claim(ClaimTypes.Role, role));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(claims), Scheme.Name)));
        }
    }
    private sealed class OrderTransportFixture : HttpMessageHandler
    {
        public string? Url, Secret, Body; public bool Confirmed = true;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Url = request.RequestUri!.AbsoluteUri; Secret = request.Headers.GetValues("X-Relay-Secret").Single(); Body = await request.Content!.ReadAsStringAsync(ct);
            return new(HttpStatusCode.OK) { Content = new StringContent(Confirmed ? "{\"status\":\"sent\"}" : "{}") };
        }
    }
}
