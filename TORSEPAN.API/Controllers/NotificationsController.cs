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
    [HttpGet("unread")]
    public async Task<IActionResult> Unread(CancellationToken ct) =>
        Ok(new { Count = await db.WorkshopMessageReceipts.CountAsync(x=>x.RecipientId==CurrentUserId && x.ReadAt==null,ct) });

    [HttpGet]
    public async Task<IActionResult> Inbox([FromQuery] int page=1, CancellationToken ct=default)
    {
        if(page<1||page>100000)return BadRequest();
        var query=db.WorkshopMessageReceipts.AsNoTracking().Where(x=>x.RecipientId==CurrentUserId);
        var total=await query.CountAsync(ct);
        var items=await query.OrderByDescending(x=>x.Message.CreatedAt).ThenBy(x=>x.MessageId)
            .Skip((page-1)*20).Take(20).Select(x=>new{
                Id=x.MessageId,x.Message.Title,x.Message.Body,x.Message.CreatedAt,x.ReadAt,
                SenderName=x.Message.Sender.FullName==""?x.Message.Sender.UserName:x.Message.Sender.FullName
            }).ToListAsync(ct);
        return Ok(new{Total=total,Page=page,PageSize=20,Items=items});
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id,CancellationToken ct)
    {
        var own=db.WorkshopMessageReceipts.Where(x=>x.MessageId==id&&x.RecipientId==CurrentUserId);
        if(!await own.AnyAsync(ct))return NotFound();
        await own.Where(x=>x.ReadAt==null).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.ReadAt,DateTime.UtcNow),ct);
        return NoContent();
    }

    [HttpGet("recipients"), Authorize(Roles="Administrator,ProductionManager")]
    public async Task<IActionResult> Recipients(CancellationToken ct) =>
        Ok(await db.Users.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.FullName)
            .Select(x=>new{x.Id,Name=x.FullName==""?x.UserName:x.FullName}).ToListAsync(ct));

    [HttpPost, Authorize(Roles="Administrator,ProductionManager")]
    public async Task<IActionResult> Send(SendWorkshopMessage request,CancellationToken ct)
    {
        var title=request.Title?.Trim();var body=request.Body?.Trim();
        if(request.Id==Guid.Empty||string.IsNullOrWhiteSpace(title)||title.Length>150||
            string.IsNullOrWhiteSpace(body)||body.Length>4000||
            (request.Broadcast&&request.RecipientId.HasValue)||(!request.Broadcast&&!request.RecipientId.HasValue))
            return BadRequest("عنوان، متن و گیرنده پیام را بررسی کنید.");
        var previous=await db.WorkshopMessages.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==request.Id,ct);
        if(previous is not null)
        {
            var same=previous.SenderId==CurrentUserId&&previous.Title==title&&previous.Body==body&&previous.Broadcast==request.Broadcast;
            if(same&&!request.Broadcast)same=await db.WorkshopMessageReceipts.AnyAsync(x=>x.MessageId==request.Id&&x.RecipientId==request.RecipientId,ct);
            return same?Ok(new{previous.Id}):Conflict("شناسه این ارسال قبلاً برای پیام دیگری استفاده شده است.");
        }
        var recipients=await db.Users.Where(x=>x.IsActive&&(request.Broadcast||x.Id==request.RecipientId))
            .Select(x=>x.Id).ToListAsync(ct);
        if(recipients.Count==0)return BadRequest("گیرنده فعالی یافت نشد.");
        db.WorkshopMessages.Add(new WorkshopMessage(request.Id,CurrentUserId,title,body,request.Broadcast));
        db.WorkshopMessageReceipts.AddRange(recipients.Select(x=>new WorkshopMessageReceipt(request.Id,x)));
        await db.SaveChangesAsync(ct);
        return Ok(new{request.Id,RecipientCount=recipients.Count});
    }
}
public sealed record SendWorkshopMessage(Guid Id,string? Title,string? Body,bool Broadcast,Guid? RecipientId);
