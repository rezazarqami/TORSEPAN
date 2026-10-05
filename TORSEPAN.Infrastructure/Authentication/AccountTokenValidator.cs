using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.Infrastructure.Authentication;
public static class AccountTokenValidator
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        if (!Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) { context.Fail("Invalid account."); return; }
        var value = context.Principal?.FindFirstValue("credential_version");
        var version = 0; // Pre-migration tokens are accepted only for unchanged accounts.
        if (value is not null && (!int.TryParse(value, out version) || version < 0)) { context.Fail("Invalid credential version."); return; }
        var db = context.HttpContext.RequestServices.GetRequiredService<TORSEPANDbContext>();
        var user = await db.Users.AsNoTracking().Where(x => x.Id == id).Select(x => new { x.IsActive, x.CredentialVersion }).SingleOrDefaultAsync(context.HttpContext.RequestAborted);
        if (user is null || !user.IsActive || user.CredentialVersion != version) context.Fail("Account credentials changed. Sign in again.");
    }
}
