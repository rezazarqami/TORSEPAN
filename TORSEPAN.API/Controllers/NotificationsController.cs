using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.API.Controllers;

[ApiController, Route("api/notifications"), Authorize]
public sealed class NotificationsController(TORSEPANDbContext db) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private IQueryable<WorkshopMessageReceipt> Conversation(Guid peer)
    {
        var me = CurrentUserId;
        return db.WorkshopMessageReceipts.AsNoTracking().Where(x => !x.Message.Broadcast &&
            ((x.Message.SenderId == me && x.RecipientId == peer) ||
             (x.Message.SenderId == peer && x.RecipientId == me)));
    }

    [HttpGet("unread")]
    public async Task<IActionResult> Unread(CancellationToken ct) =>
        Ok(new { Count = await db.WorkshopMessageReceipts.CountAsync(x => x.RecipientId == CurrentUserId && x.ReadAt == null, ct) });

    // Keep the original inbox route compatible with older clients.
    [HttpGet]
    public async Task<IActionResult> Inbox([FromQuery] int page = 1, CancellationToken ct = default)
    {
        if (page < 1 || page > 100000) return BadRequest();
        var query = db.WorkshopMessageReceipts.AsNoTracking().Where(x => x.RecipientId == CurrentUserId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.Message.CreatedAt).ThenBy(x => x.MessageId)
            .Skip((page - 1) * 20).Take(20).Select(x => new {
                Id = x.MessageId, x.Message.Title, x.Message.Body, x.Message.CreatedAt, x.ReadAt,
                SenderName = x.Message.Sender.FullName == "" ? x.Message.Sender.UserName : x.Message.Sender.FullName
            }).ToListAsync(ct);
        return Ok(new { Total = total, Page = page, PageSize = 20, Items = items });
    }

    [HttpGet("announcements")]
    public async Task<IActionResult> Announcements([FromQuery] int page = 1, CancellationToken ct = default)
    {
        if (page < 1 || page > 100000) return BadRequest();
        var me = CurrentUserId;
        var query = db.WorkshopMessages.AsNoTracking().Where(x => x.Broadcast &&
            (x.SenderId == me || db.WorkshopMessageReceipts.Any(r => r.MessageId == x.Id && r.RecipientId == me)));
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * 20).Take(20).Select(x => new {
                x.Id, x.Title, x.Body, x.CreatedAt, IsMine = x.SenderId == me,
                ReadAt = db.WorkshopMessageReceipts.Where(r => r.MessageId == x.Id && r.RecipientId == me).Select(r => r.ReadAt).FirstOrDefault(),
                SenderName = x.Sender.FullName == "" ? x.Sender.UserName : x.Sender.FullName
            }).ToListAsync(ct);
        return Ok(new { Total = total, Page = page, PageSize = 20, Items = items });
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> Conversations(CancellationToken ct)
    {
        var me = CurrentUserId;
        var direct = db.WorkshopMessageReceipts.AsNoTracking().Where(r => !r.Message.Broadcast);
        var items = await db.Users.AsNoTracking().Where(u => u.Id != me &&
            (u.IsActive || direct.Any(r => (r.Message.SenderId == me && r.RecipientId == u.Id) || (r.Message.SenderId == u.Id && r.RecipientId == me))))
            .Select(u => new {
                u.Id, Name = u.FullName == "" ? u.UserName : u.FullName, u.IsActive,
                UnreadCount = direct.Count(r => r.RecipientId == me && r.Message.SenderId == u.Id && r.ReadAt == null),
                LastBody = direct.Where(r => (r.Message.SenderId == me && r.RecipientId == u.Id) || (r.Message.SenderId == u.Id && r.RecipientId == me))
                    .OrderByDescending(r => r.Message.CreatedAt).ThenByDescending(r => r.MessageId).Select(r => r.Message.Body).FirstOrDefault(),
                LastAt = direct.Where(r => (r.Message.SenderId == me && r.RecipientId == u.Id) || (r.Message.SenderId == u.Id && r.RecipientId == me))
                    .Max(r => (DateTime?)r.Message.CreatedAt)
            }).OrderByDescending(x => x.LastAt).ThenBy(x => x.Name).ToListAsync(ct);
        return Ok(new { Items = items });
    }

    [HttpGet("conversations/{peer:guid}")]
    public async Task<IActionResult> History(Guid peer, [FromQuery] int page = 1, CancellationToken ct = default)
    {
        if (peer == CurrentUserId || page < 1 || page > 100000) return BadRequest();
        if (!await db.Users.AnyAsync(x => x.Id == peer, ct)) return NotFound();
        var me = CurrentUserId;
        var query = Conversation(peer);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.Message.CreatedAt).ThenByDescending(x => x.MessageId)
            .Skip((page - 1) * 50).Take(50).Select(x => new {
                Id = x.MessageId, x.Message.Title, x.Message.Body, x.Message.CreatedAt, x.ReadAt,
                IsMine = x.Message.SenderId == me
            }).ToListAsync(ct);
        items.Reverse();
        return Ok(new { Items = items, Page = page, PageSize = 50, Total = total });
    }

    [HttpPost("conversations/{peer:guid}/read")]
    public async Task<IActionResult> ReadConversation(Guid peer, ReadChatMessages request, CancellationToken ct)
    {
        if (peer == CurrentUserId || request.Ids is null || request.Ids.Count == 0 || request.Ids.Count > 50) return BadRequest();
        var ids = request.Ids.Distinct().ToArray();
        var now = DateTime.UtcNow;
        var changed = await db.WorkshopMessageReceipts.Where(x => x.RecipientId == CurrentUserId &&
            x.Message.SenderId == peer && !x.Message.Broadcast && x.ReadAt == null && ids.Contains(x.MessageId))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReadAt, now), ct);
        return Ok(new { Changed = changed, ReadAt = now });
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var own = db.WorkshopMessageReceipts.Where(x => x.MessageId == id && x.RecipientId == CurrentUserId);
        if (!await own.AnyAsync(ct)) return NotFound();
        await own.Where(x => x.ReadAt == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.ReadAt, DateTime.UtcNow), ct);
        return NoContent();
    }

    [HttpGet("recipients")]
    public async Task<IActionResult> Recipients(CancellationToken ct) =>
        Ok(await db.Users.AsNoTracking().Where(x => x.IsActive && x.Id != CurrentUserId).OrderBy(x => x.FullName)
            .Select(x => new { x.Id, Name = x.FullName == "" ? x.UserName : x.FullName }).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Send(SendWorkshopMessage request, CancellationToken ct)
    {
        var title = request.Title?.Trim(); var body = request.Body?.Trim();
        if (!request.Broadcast && string.IsNullOrWhiteSpace(title)) title = "گفتگو";
        if (request.Id == Guid.Empty || string.IsNullOrWhiteSpace(title) || title.Length > 150 ||
            string.IsNullOrWhiteSpace(body) || body.Length > 4000 ||
            (request.Broadcast && request.RecipientId.HasValue) || (!request.Broadcast && !request.RecipientId.HasValue) || request.RecipientId == CurrentUserId)
            return BadRequest("عنوان، متن و گیرنده پیام را بررسی کنید.");
        if (request.Broadcast && !User.IsInRole("Administrator") && !User.IsInRole("ProductionManager")) return Forbid();
        var previous = await db.WorkshopMessages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.Id, ct);
        if (previous is not null) return await Existing(previous);
        if (!await db.Users.AnyAsync(x => x.Id == CurrentUserId && x.IsActive, ct)) return Forbid();
        var recipients = await db.Users.Where(x => x.IsActive && x.Id != CurrentUserId && (request.Broadcast || x.Id == request.RecipientId))
            .Select(x => x.Id).ToListAsync(ct);
        if (!request.Broadcast && recipients.Count == 0) return BadRequest("گیرنده فعالی یافت نشد.");
        db.WorkshopMessages.Add(new WorkshopMessage(request.Id, CurrentUserId, title, body, request.Broadcast));
        db.WorkshopMessageReceipts.AddRange(recipients.Select(x => new WorkshopMessageReceipt(request.Id, x)));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            // The primary key also protects concurrent retries of the same send.
            db.ChangeTracker.Clear();
            var stored = await db.WorkshopMessages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.Id, ct);
            if (stored is null) throw;
            return await Existing(stored);
        }
        return Ok(new { request.Id, RecipientCount = recipients.Count });

        async Task<IActionResult> Existing(WorkshopMessage stored)
        {
            var same = stored.SenderId == CurrentUserId && stored.Title == title && stored.Body == body && stored.Broadcast == request.Broadcast;
            if (same && !request.Broadcast) same = await db.WorkshopMessageReceipts.AnyAsync(x => x.MessageId == request.Id && x.RecipientId == request.RecipientId, ct);
            return same ? Ok(new { stored.Id }) : Conflict("شناسه این ارسال قبلاً برای پیام دیگری استفاده شده است.");
        }
    }
}
public sealed record SendWorkshopMessage(Guid Id, string? Title, string? Body, bool Broadcast, Guid? RecipientId);
public sealed record ReadChatMessages(IReadOnlyCollection<Guid>? Ids);
