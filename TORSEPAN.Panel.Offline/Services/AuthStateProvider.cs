using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
namespace TORSEPAN.Panel.Services.Auth;
public sealed class AuthStateProvider(IJSRuntime js):AuthenticationStateProvider{
 public override async Task<AuthenticationState> GetAuthenticationStateAsync(){var s=await js.InvokeAsync<Session>("OfflinePanel.session");var claims=new List<Claim>();if(s.Ready){claims.Add(new(ClaimTypes.NameIdentifier,s.OwnerId));claims.Add(new(ClaimTypes.Name,s.UserName));claims.Add(new("FullName",s.FullName));claims.Add(new("Title",s.Title??""));foreach(var role in s.Roles??[])claims.Add(new(ClaimTypes.Role,role));}return new(new ClaimsPrincipal(new ClaimsIdentity(claims,s.Ready?"cached-workshop":null)));}
 public async Task<bool> RefreshAsync(){NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());return (await GetAuthenticationStateAsync()).User.Identity?.IsAuthenticated==true;}
 public record Session(bool Ready,string OwnerId,string UserName,string FullName,string? Title,string[]? Roles);
}
