using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.API.Services;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Infrastructure.Persistence;
namespace TORSEPAN.API.Controllers;

[ApiController, Route("api/notifications/push"), Authorize]
public sealed class MessagePushController(TORSEPANDbContext db, MessagePushKeyStore keys) : ControllerBase
{
    private Guid Me => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("key")]
    public async Task<IActionResult> Key(CancellationToken ct) => Ok(new { (await keys.GetAsync(ct)).PublicKey, UserId = Me });

    [HttpPut, EnableRateLimiting("account-security")]
    public async Task<IActionResult> Subscribe(SubscribeMessagePush request, CancellationToken ct)
    {
        if (!MessagePushValidation.Endpoint(request.Endpoint) || !MessagePushValidation.Key(request.P256dh, 65) || !MessagePushValidation.Key(request.Auth, 16))
            return BadRequest(new { Message = "اطلاعات اعلان این مرورگر معتبر نیست." });
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == Me && x.IsActive && !x.IsDeleted, ct);
        if (user is null) return Unauthorized();
        var hash = MessagePushValidation.Hash(request.Endpoint);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // One physical browser subscription belongs to one current account; rebinding drops old pending notifications.
        var existing = await db.MessagePushSubscriptions.SingleOrDefaultAsync(x => x.EndpointHash == hash, ct);
        if (existing is not null && (existing.UserId != Me || existing.CredentialVersion != user.CredentialVersion))
            await db.MessagePushDeliveries.Where(x => x.SubscriptionId == existing.Id).ExecuteDeleteAsync(ct);
        var id = existing?.Id ?? Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"MessagePushSubscriptions\" (\"Id\",\"UserId\",\"CredentialVersion\",\"Endpoint\",\"EndpointHash\",\"P256dh\",\"Auth\",\"UpdatedAt\") VALUES ({id},{Me},{user.CredentialVersion},{request.Endpoint},{hash},{request.P256dh},{request.Auth},{DateTime.UtcNow}) ON CONFLICT (\"EndpointHash\") DO UPDATE SET \"UserId\"=EXCLUDED.\"UserId\",\"CredentialVersion\"=EXCLUDED.\"CredentialVersion\",\"P256dh\"=EXCLUDED.\"P256dh\",\"Auth\"=EXCLUDED.\"Auth\",\"UpdatedAt\"=EXCLUDED.\"UpdatedAt\"", ct);
        await tx.CommitAsync(ct); return NoContent();
    }
    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe(UnsubscribeMessagePush request, CancellationToken ct)
    {
        if (!MessagePushValidation.Endpoint(request.Endpoint)) return BadRequest();
        var hash = MessagePushValidation.Hash(request.Endpoint);
        await db.MessagePushSubscriptions.Where(x => x.UserId == Me && x.EndpointHash == hash).ExecuteDeleteAsync(ct);
        return NoContent();
    }
}
public sealed record SubscribeMessagePush(string Endpoint, string P256dh, string Auth);
public sealed record UnsubscribeMessagePush(string Endpoint);
