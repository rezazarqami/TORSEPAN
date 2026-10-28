using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.API.Services;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Infrastructure.Persistence;
namespace TORSEPAN.API.Controllers;

[ApiController,Route("api/notifications/mobile"),Authorize]
public sealed class NativeMessagePushController(TORSEPANDbContext db,NativeMessagePushConfiguration config):ControllerBase
{
    private Guid Me=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("config")]
    public IActionResult Config([FromQuery]string? app=null){var client=app=="online"?config.OnlineClient:config.Client;return Ok(new{configured=client is not null,client,userId=Me});}
    [HttpPut,EnableRateLimiting("account-security")]
    public async Task<IActionResult> Register(NativeMessageEnrollment request,CancellationToken ct)
    {
        if(!config.Configured)return StatusCode(503,new{message="اعلان گوشی هنوز روی سرور فعال نشده است."});
        if(request.DeviceId==Guid.Empty||request.EnrollmentId==Guid.Empty||!NativeMessagePushConfiguration.ValidToken(request.Token))return BadRequest();
        var user=await db.Users.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==Me&&x.IsActive&&!x.IsDeleted,ct);if(user is null)return Unauthorized();
        var endpoint=NativeMessagePushConfiguration.Prefix+request.Token;var hash=MessagePushValidation.Hash(endpoint);var device=request.DeviceId.ToString();
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var existing=await db.MessagePushSubscriptions.Where(x=>x.EndpointHash==hash||(x.Endpoint.StartsWith(NativeMessagePushConfiguration.Prefix)&&x.P256dh==device)).ToListAsync(ct);
        var keep=existing.FirstOrDefault(x=>x.EndpointHash==hash);var id=keep?.Id??Guid.NewGuid();
        foreach(var old in existing)
        {
            if(old.Id!=id){db.MessagePushSubscriptions.Remove(old);continue;}
            if(old.UserId!=Me||old.CredentialVersion!=user.CredentialVersion||old.Auth!=request.EnrollmentId.ToString())await db.MessagePushDeliveries.Where(x=>x.SubscriptionId==old.Id).ExecuteDeleteAsync(ct);
        }
        if(keep is null){keep=new MessagePushSubscription{Id=id};db.MessagePushSubscriptions.Add(keep);}
        keep.UserId=Me;keep.CredentialVersion=user.CredentialVersion;keep.Endpoint=endpoint;keep.EndpointHash=hash;keep.P256dh=device;keep.Auth=request.EnrollmentId.ToString();keep.UpdatedAt=DateTime.UtcNow;
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return NoContent();
    }
    [HttpDelete("{deviceId:guid}")]
    public async Task<IActionResult> Remove(Guid deviceId,CancellationToken ct)
    {
        var device=deviceId.ToString();await db.MessagePushSubscriptions.Where(x=>x.UserId==Me&&x.Endpoint.StartsWith(NativeMessagePushConfiguration.Prefix)&&x.P256dh==device).ExecuteDeleteAsync(ct);return NoContent();
    }
}
public sealed record NativeMessageEnrollment(Guid DeviceId,Guid EnrollmentId,string Token);
