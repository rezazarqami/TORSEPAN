using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TORSEPAN.API.Security;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.API.Controllers;
[ApiController, Route("api/me/profile"), Authorize]
public sealed class MyProfileController(TORSEPANDbContext db) : ControllerBase
{
    private Guid Me => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await db.Users.AsNoTracking().Where(x => x.Id == Me)
        .Select(x => new { x.Id, x.UserName, x.FullName, x.Title, x.AvatarVersion }).SingleAsync(ct));

    [HttpPut("credentials"), EnableRateLimiting("account-security")]
    public async Task<IActionResult> Credentials(ChangeOwnCredentials request, CancellationToken ct)
    {
        if (request.CurrentPassword is null || request.CurrentPassword.Length is 0 or > 1024) return BadRequest(new { Code = "current-password", Message = "رمز فعلی را وارد کنید." });
        var name = request.UserName?.Trim().Normalize(NormalizationForm.FormKC);
        if (name is null || name.Length is < 3 or > 100 || name.Any(c => !char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '.'))
            return BadRequest(new { Code = "username", Message = "نام کاربری باید ۳ تا ۱۰۰ حرف یا عدد باشد؛ نقطه، خط تیره و زیرخط هم مجازند." });
        var changePassword = !string.IsNullOrEmpty(request.NewPassword);
        if ((!changePassword && !string.IsNullOrEmpty(request.ConfirmPassword)) || changePassword && (request.NewPassword!.Length is < 6 or > 128 || request.NewPassword != request.ConfirmPassword))
            return BadRequest(new { Code = "new-password", Message = "رمز جدید باید ۶ تا ۱۲۸ کاراکتر باشد و با تکرارش یکسان باشد." });
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == Me && !x.IsDeleted && x.IsActive, ct);
        if (user is null) return Unauthorized();
        if (!user.VerifyPassword(request.CurrentPassword)) return BadRequest(new { Code = "current-password", Message = "رمز فعلی درست نیست." });
        if (await db.Users.AnyAsync(x => x.Id != Me && x.UserName.Trim().ToUpper() == name.ToUpper(), ct))
            return Conflict(new { Code = "duplicate-username", Message = "این نام کاربری قبلاً استفاده شده است." });
        var changed = user.UserName != name || changePassword;
        if (!changed) return Ok(new { Changed = false });
        user.ChangeUserName(name);
        // Rotate both password and username changes so old name claims cannot linger.
        user.ChangePassword(changePassword ? request.NewPassword! : request.CurrentPassword);
        foreach (var token in await db.RefreshTokens.Where(x => x.UserId == Me && !x.Revoked).ToListAsync(ct)) token.Revoke();
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { Code = "concurrent-change", Message = "حساب هم‌زمان تغییر کرده است؛ دوباره وارد شوید." }); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { return Conflict(new { Code = "duplicate-username", Message = "این نام کاربری قبلاً استفاده شده است." }); }
        return Ok(new { Changed = true });
    }

    [HttpPut("avatar"), RequestSizeLimit(190_000)]
    public async Task<IActionResult> Avatar(OwnAvatar request, CancellationToken ct)
    {
        byte[]? image = null;
        if (request.PngBase64 is not null)
        {
            if (request.PngBase64.Length > 174_764) return BadRequest(new { Code = "avatar", Message = "حجم عکس زیاد است؛ عکس کوچک‌تری انتخاب کنید." });
            try { image = ProfilePng.Sanitize(Convert.FromBase64String(request.PngBase64)); }
            catch (Exception e) when (e is FormatException or InvalidDataException or EndOfStreamException)
            { return BadRequest(new { Code = "avatar", Message = "عکس معتبر نیست؛ یک عکس دیگر انتخاب کنید." }); }
        }
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == Me && !x.IsDeleted && x.IsActive, ct);
        if (user is null) return Unauthorized();
        user.ChangeAvatar(image);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { Code = "concurrent-change", Message = "حساب هم‌زمان تغییر کرده است؛ صفحه را به‌روز کنید." }); }
        return Ok(new { user.AvatarVersion });
    }

    [HttpGet("/api/users/{id:guid}/avatar")]
    public async Task<IActionResult> Image(Guid id, CancellationToken ct)
    {
        var photo = await db.Users.AsNoTracking().Where(x => x.Id == id).Select(x => new { x.AvatarPng, x.AvatarVersion }).SingleOrDefaultAsync(ct);
        Response.Headers.CacheControl = "private, no-store"; Response.Headers["X-Content-Type-Options"] = "nosniff";
        return photo?.AvatarPng is null ? NotFound() : File(photo.AvatarPng, "image/png");
    }
}
public sealed record ChangeOwnCredentials(string UserName, string CurrentPassword, string? NewPassword, string? ConfirmPassword);
public sealed record OwnAvatar(string? PngBase64);

