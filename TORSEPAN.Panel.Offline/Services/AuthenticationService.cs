using Microsoft.JSInterop;
using TORSEPAN.Application.Auth.Commands.Login;
namespace TORSEPAN.Panel.Services.Auth;
public sealed class AuthenticationService(IJSRuntime js):IAuthService{
 public bool IsAuthenticated=>true;public string? Token=>null;public string? UserName=>null;public string? FullName=>null;public IReadOnlyList<string> Roles=>["Admin","Administrator"];
 public Task<LoginResult> LoginAsync(LoginCommand command)=>throw new InvalidOperationException("برای ورود به حساب، نسخهٔ وب را باز کنید.");
 public async Task LogoutAsync()=>await js.InvokeVoidAsync("OfflinePanel.online");
 public Task<bool> RenewSessionAsync()=>Task.FromResult(false);
}
