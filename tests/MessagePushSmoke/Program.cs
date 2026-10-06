using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TORSEPAN.API.Controllers;
using TORSEPAN.API.Services;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Infrastructure.Persistence;

void Check(bool value, string text) { if (!value) throw new Exception(text); Console.WriteLine("PASS " + text); }
using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
var transport = new FixtureTransport();
var services = new ServiceCollection(); services.AddLogging();
services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
services.AddDbContext<TORSEPANDbContext>(o => o.UseSqlite(connection)); services.AddScoped<MessagePushKeyStore>(); services.AddSingleton<IMessagePushTransport>(transport);
await using var provider = services.BuildServiceProvider(); await using var scope = provider.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<TORSEPANDbContext>(); await db.Database.EnsureCreatedAsync();
var sender = new User("push-sender", "فرستنده"); var recipient = new User("push-recipient", "گیرنده"); var other = new User("push-other", "عضو دیگر");
db.Users.AddRange(sender, recipient, other); await db.SaveChangesAsync();
var keyStore = scope.ServiceProvider.GetRequiredService<MessagePushKeyStore>();
var firstKey = await keyStore.GetAsync(default); var secondKey = await keyStore.GetAsync(default);
Check(firstKey.PublicKey == secondKey.PublicKey && await db.MessagePushKeys.CountAsync() == 1, "VAPID identity persists across repeated initialization");
var wire = new FixtureHttp(); var realTransport = new WebMessagePushTransport(new HttpClient(wire));
await realTransport.SendAsync(new MessagePushSubscription { Endpoint = "https://web.push.apple.com/fixture", P256dh = firstKey.PublicKey, Auth = Convert.ToBase64String(new byte[16]).TrimEnd('=') }, firstKey, "https://torsepan.liara.run", "encrypted-fixture", default);
Check(wire.Encoding == "aes128gcm" && wire.Authorization.StartsWith("vapid ") && wire.Payload.Length > 80 && !System.Text.Encoding.UTF8.GetString(wire.Payload).Contains("encrypted-fixture"), "actual transport uses encrypted RFC 8291 aes128gcm and modern VAPID authentication for Apple Push");
var jwt = wire.Authorization.Split("t=")[1].Split(',')[0];
using (var claims = JsonDocument.Parse(MessagePushValidation.Decode(jwt.Split('.')[1])))
    Check(claims.RootElement.GetProperty("aud").GetString() == "https://web.push.apple.com" && claims.RootElement.GetProperty("sub").GetString() == "https://torsepan.liara.run", "VAPID token uses the correct push-provider audience and application subject");
ControllerContext As(Guid id) => new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString())], "fixture")) } };
MessagePushController Push(Guid id) => new(db, keyStore) { ControllerContext = As(id) };
var browserKey = firstKey.PublicKey; var auth = Convert.ToBase64String(new byte[16]).TrimEnd('=').Replace('+','-').Replace('/','_');
SubscribeMessagePush Sub(string tail) => new("https://fcm.googleapis.com/fcm/send/" + tail, browserKey, auth);
Check(await Push(recipient.Id).Subscribe(Sub("device-1"), default) is NoContentResult, "authenticated device subscription is saved");
Check(await Push(recipient.Id).Subscribe(Sub("device-1"), default) is NoContentResult && await db.MessagePushSubscriptions.CountAsync() == 1, "registering the same browser is idempotent");
Check(await Push(recipient.Id).Subscribe(new("https://127.0.0.1/private", browserKey, auth), default) is BadRequestObjectResult, "private and arbitrary endpoints are rejected");
foreach (var url in new[] { "http://fcm.googleapis.com/x", "https://fcm.googleapis.com.attacker.test/x", "https://attacker.test/x", "https://fcm.googleapis.com:444/x", "https://name@fcm.googleapis.com/x" })
    Check(!MessagePushValidation.Endpoint(url), "unsafe endpoint rejected: " + url);
Check(MessagePushValidation.Endpoint("https://web.push.apple.com/Qtoken") && MessagePushValidation.Endpoint("https://updates.push.services.mozilla.com/wpush/v2/token"), "Apple and Firefox push providers are supported");
Check(await Push(recipient.Id).Subscribe(new(Sub("bad-key").Endpoint, "broken", auth), default) is BadRequestObjectResult, "malformed browser crypto key is rejected");
await Push(recipient.Id).Subscribe(Sub("device-2"), default);
var notifications = new NotificationsController(db) { ControllerContext = As(sender.Id) };
var message = new SendWorkshopMessage(Guid.NewGuid(), null, "private contents should not appear on lock screen", false, recipient.Id);
Check(await notifications.Send(message, default) is OkObjectResult && await db.MessagePushDeliveries.CountAsync() == 2, "message and a delivery for each subscribed recipient device are saved together");
await notifications.Send(message, default);
Check(await db.WorkshopMessages.CountAsync() == 1 && await db.MessagePushDeliveries.CountAsync() == 2, "retrying an acknowledged message does not queue duplicate pushes");
var worker = new MessagePushWorker(provider.GetRequiredService<IServiceScopeFactory>(), provider.GetRequiredService<ILogger<MessagePushWorker>>());
await worker.DispatchAsync(default);
Check(transport.Payloads.Count == 2 && await db.MessagePushDeliveries.CountAsync() == 0, "a new worker scope drains persisted deliveries for both devices");
using (var payload = JsonDocument.Parse(transport.Payloads[0]))
    Check(payload.RootElement.GetProperty("userId").GetGuid() == recipient.Id && payload.RootElement.GetProperty("totalIncoming").GetInt64() == 1 && !transport.Payloads[0].Contains(message.Body!), "push is addressed to the recipient with no private message preview");
await notifications.Send(message with { Id = Guid.NewGuid() }, default); transport.Failure = HttpStatusCode.ServiceUnavailable;
await worker.DispatchAsync(default);
Check(await db.MessagePushDeliveries.CountAsync() == 2 && await db.MessagePushDeliveries.AllAsync(x => x.Attempts == 1 && x.NextAttemptAt > DateTime.UtcNow), "temporary delivery failure schedules retry without losing the message");
transport.Failure = null; await db.MessagePushDeliveries.ExecuteUpdateAsync(s => s.SetProperty(x => x.NextAttemptAt, DateTime.UtcNow.AddMinutes(-1)));
await worker.DispatchAsync(default);
Check(await db.MessagePushDeliveries.CountAsync() == 0 && transport.Payloads.Count == 4, "retry completes retained deliveries");
await notifications.Send(message with { Id = Guid.NewGuid() }, default);
await Push(other.Id).Subscribe(Sub("device-1"), default);
var firstEndpointHash = MessagePushValidation.Hash(Sub("device-1").Endpoint);
Check(await db.MessagePushDeliveries.CountAsync() == 1 && await db.MessagePushSubscriptions.AsNoTracking().AnyAsync(x => x.EndpointHash == firstEndpointHash && x.UserId == other.Id), "changing the account on a device removes the previous account's pending notification");
await Push(other.Id).Unsubscribe(new(Sub("device-2").Endpoint), default);
Check(await db.MessagePushSubscriptions.CountAsync() == 2, "a user cannot unsubscribe another account's endpoint");
recipient.ChangePassword("new-push-password"); await db.SaveChangesAsync(); var sent = transport.Payloads.Count;
await worker.DispatchAsync(default);
Check(transport.Payloads.Count == sent && await db.MessagePushDeliveries.CountAsync() == 0, "password rotation invalidates pending notifications for older sessions");
await Push(recipient.Id).Subscribe(Sub("device-2"), default);
await notifications.Send(message with { Id = Guid.NewGuid() }, default); transport.Failure = HttpStatusCode.Gone;
await worker.DispatchAsync(default);
Check(!await db.MessagePushSubscriptions.AnyAsync(x => x.UserId == recipient.Id) && await db.MessagePushDeliveries.CountAsync() == 0, "expired push endpoints and their pending deliveries are cleaned up");
await Push(other.Id).Unsubscribe(new(Sub("device-1").Endpoint), default);
Check(await db.MessagePushSubscriptions.CountAsync() == 0, "explicit logout or opt-out removes the current device subscription");
await using var postgres = new TORSEPANDbContext(new DbContextOptionsBuilder<TORSEPANDbContext>().UseNpgsql("Host=fixture.invalid;Database=fixture").Options);
var sql = postgres.GetService<IMigrator>().GenerateScript("20261006110000_AddUserDeletion", "20261006133310_AddMessagePush");
Check(sql.Contains("CREATE TABLE \"MessagePushSubscriptions\"") && sql.Contains("CREATE TABLE \"MessagePushKeys\"") && !sql.Contains("DROP TABLE"), "PostgreSQL migration is additive and includes persistent device/key tables");
Console.WriteLine("Message push smoke passed; external push providers were not contacted.");
sealed class FixtureTransport : IMessagePushTransport
{
    public List<string> Payloads = []; public HttpStatusCode? Failure;
    public Task SendAsync(MessagePushSubscription subscription, MessagePushKeys keys, string subject, string payload, CancellationToken ct)
    { if (Failure.HasValue) throw new MessagePushTransportException(Failure.Value); Payloads.Add(payload); return Task.CompletedTask; }
}
sealed class FixtureHttp : HttpMessageHandler
{
    public string Encoding = "", Authorization = ""; public byte[] Payload = [];
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Encoding = request.Content!.Headers.ContentEncoding.Single(); Authorization = request.Headers.Authorization!.ToString();
        Payload = await request.Content.ReadAsByteArrayAsync(ct); return new(HttpStatusCode.Created);
    }
}
