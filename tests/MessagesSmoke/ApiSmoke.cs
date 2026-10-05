using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TORSEPAN.API.Controllers;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Panel.Models;

internal static class ApiSmoke
{
    public static async Task RunAsync()
    {
        var connection = Environment.GetEnvironmentVariable("MESSAGES_TEST_DB") ?? throw new Exception("Isolated PostgreSQL fixture is required");
        var options = new DbContextOptionsBuilder<TORSEPANDbContext>().UseNpgsql(connection).Options;
        await using var db = new TORSEPANDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var users = Enumerable.Range(0, 10).Select(i => new User("message-test-" + i, "عضو کارگاه " + i)).ToArray();
        db.Users.AddRange(users); await db.SaveChangesAsync();
        NotificationsController As(int index, string role = "Tuner", TORSEPANDbContext? context = null) => new(context ?? db)
        {
            ControllerContext = new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, users[index].Id.ToString()), new Claim(ClaimTypes.Role, role)], "fixture")) } }
        };
        var sender = As(0); var recipient = As(1); var outsider = As(2);
        var roster = Value<ConversationListDto>(await sender.Conversations(default));
        Check(roster.Items.Count == 9 && roster.Items.All(x => x.Id != users[0].Id), "ten members produce nine independent conversations excluding the current user");
        var id = Guid.NewGuid(); var request = new SendWorkshopMessage(id, null, "سلام همکار", false, users[1].Id);
        Check(await sender.Send(request, default) is OkObjectResult, "ordinary workshop members can send private messages");
        Check(await sender.Send(request, default) is OkObjectResult && await db.WorkshopMessages.CountAsync(x => x.Id == id) == 1, "retry of an acknowledged send does not duplicate the message");
        Check(await sender.Send(request with { Body = "متن متفاوت" }, default) is ConflictObjectResult, "reusing a send ID for different content is rejected");
        var sent = Value<ChatHistoryDto>(await sender.History(users[1].Id, 1));
        Check(sent.Items.Single().IsMine && sent.Items.Single().ReadAt is null, "sender history includes the persisted sent message before recipient reading");
        Check(Value<ChatHistoryDto>(await outsider.History(users[1].Id, 1)).Items.Count == 0, "a third member cannot read another pair's conversation");
        Check(await sender.MarkRead(id, default) is NotFoundResult, "a sender cannot mark their outgoing message as read by its recipient");
        await outsider.ReadConversation(users[0].Id, new([id]), default);
        Check((await db.WorkshopMessageReceipts.AsNoTracking().SingleAsync(x => x.MessageId == id)).ReadAt is null, "a third member cannot change someone else's receipt");
        var another = Guid.NewGuid(); await sender.Send(request with { Id = another, Body = "پیام دوم" }, default);
        await recipient.ReadConversation(users[0].Id, new([id]), default);
        var statuses = Value<ChatHistoryDto>(await sender.History(users[1].Id, 1)).Items;
        Check(statuses.Single(x => x.Id == id).ReadAt.HasValue && statuses.Single(x => x.Id == another).ReadAt is null, "only explicitly visible incoming messages receive read receipts");
        Check(await sender.Send(new(Guid.NewGuid(), "اطلاعیه", "عمومی", true, null), default) is ForbidResult, "ordinary members cannot publish announcements");
        var manager = As(0, "ProductionManager"); var broadcastId = Guid.NewGuid();
        Check(await manager.Send(new(broadcastId, "برنامه کارگاه", "جلسه فردا", true, null), default) is OkObjectResult, "workshop manager can publish to the announcement channel");
        Check(await db.WorkshopMessageReceipts.CountAsync(x => x.MessageId == broadcastId) == 9, "announcement reaches all other active members without self-unread receipts");
        Check(Value<InboxDto>(await recipient.Announcements()).Items.Select(x => x.Id).SequenceEqual([broadcastId]), "announcement channel excludes private messages");
        Check(Value<InboxDto>(await manager.Announcements()).Items.Any(x => x.Id == broadcastId && x.IsMine), "author can read their own announcement archive");
        Check(Value<ChatHistoryDto>(await sender.History(users[1].Id, 1)).Items.All(x => x.Id != broadcastId), "private conversation excludes announcements");
        Check(await sender.Send(new(Guid.NewGuid(), null, "خودم", false, users[0].Id), default) is BadRequestObjectResult, "self messaging is rejected");
        users[1].Deactivate(); await db.SaveChangesAsync();
        Check(await sender.Send(request with { Id = Guid.NewGuid() }, default) is BadRequestObjectResult, "inactive member cannot receive new messages");
        Check(Value<ConversationListDto>(await sender.Conversations(default)).Items.Any(x => x.Id == users[1].Id && !x.IsActive), "inactive member's existing conversation remains archived");
        users[1].Activate(); await db.SaveChangesAsync();
        for (var i = 0; i < 105; i++)
        {
            var message = new WorkshopMessage(Guid.NewGuid(), users[0].Id, "گفتگو", "آرشیو " + i, false);
            db.WorkshopMessages.Add(message); db.WorkshopMessageReceipts.Add(new(message.Id, users[1].Id));
        }
        await db.SaveChangesAsync();
        var first = Value<ChatHistoryDto>(await sender.History(users[1].Id, 1));
        var second = Value<ChatHistoryDto>(await sender.History(users[1].Id, 2));
        Check(first.Items.Count == 50 && second.Items.Count == 50 && !first.Items.Select(x => x.Id).Intersect(second.Items.Select(x => x.Id)).Any(), "long conversation history is paginated without cross-page duplicates");
        var parallelId = Guid.NewGuid();
        await using var leftDb = new TORSEPANDbContext(options); await using var rightDb = new TORSEPANDbContext(options);
        var duplicate = request with { Id = parallelId, Body = "همزمان" };
        var results = await Task.WhenAll(As(0, context: leftDb).Send(duplicate, default), As(0, context: rightDb).Send(duplicate, default));
        Check(results.All(x => x is OkObjectResult) && await db.WorkshopMessages.CountAsync(x => x.Id == parallelId) == 1, "concurrent retries persist one message and both acknowledge it");
        await VerifyAuthorizationAsync();
    }
    public static T Value<T>(IActionResult result) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(((OkObjectResult)result).Value), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    private static async Task VerifyAuthorizationAsync()
    {
        using var services = new ServiceCollection().AddLogging().AddAuthorizationCore().BuildServiceProvider();
        var provider = services.GetRequiredService<IAuthorizationPolicyProvider>(); var authorization = services.GetRequiredService<IAuthorizationService>();
        foreach (var name in new[] { "Conversations", "History", "ReadConversation", "Send", "Recipients", "Announcements" })
        {
            var attributes = typeof(NotificationsController).GetCustomAttributes<AuthorizeAttribute>().Cast<IAuthorizeData>().Concat(typeof(NotificationsController).GetMethod(name)!.GetCustomAttributes<AuthorizeAttribute>());
            var policy = (await AuthorizationPolicy.CombineAsync(provider, attributes))!;
            foreach (var role in new[] { "Administrator", "ProductionManager", "Tuner", "Workshop", "Warehouse", "SalesAdmin", "anonymous" })
            {
                var user = role == "anonymous" ? new ClaimsPrincipal(new ClaimsIdentity()) : new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "fixture"));
                Check((await authorization.AuthorizeAsync(user, null, policy)).Succeeded == (role != "anonymous"), name + " respects authenticated membership for " + role);
            }
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); Console.WriteLine("PASS " + message); }
}
