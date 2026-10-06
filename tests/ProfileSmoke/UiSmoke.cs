using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using TORSEPAN.Application.Auth.Commands.Login;
using TORSEPAN.Panel.Components.Pages;
using TORSEPAN.Panel.Components.Shared;
using TORSEPAN.Panel.Models;
using TORSEPAN.Panel.Services;
using TORSEPAN.Panel.Services.Api;
using TORSEPAN.Panel.Services.Auth;

internal static class UiSmoke
{
 public static async Task RunAsync()
 {
  var output=Environment.GetEnvironmentVariable("PROFILE_PREVIEW_DIR");if(output is not null)Directory.CreateDirectory(output);
  var api=new ProfileFixtureApi();var auth=new ProfileAuth();var nav=new ProfileNavigation();var services=new ServiceCollection();
  services.AddLogging();services.AddAuthorizationCore();services.AddCascadingAuthenticationState();services.AddSingleton<AuthenticationStateProvider>(new ProfileState());services.AddSingleton<IJSRuntime>(new ProfileJs());services.AddScoped<TokenStorage>();services.AddSingleton<IAuthService>(auth);services.AddSingleton<NavigationManager>(nav);
  services.AddSingleton(new HttpClient(api){BaseAddress=new Uri("https://fixture.invalid/api/")});services.AddScoped<ApiClient>();services.AddScoped<PersonalWorkspaceService>();
  await using var provider=services.BuildServiceProvider();await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
  await renderer.Dispatcher.InvokeAsync(async()=>
  {
   var rendered=await renderer.RenderComponentAsync<ProfilePreview>(ParameterView.Empty);var page=ProfilePreview.Instance!;
   string Html()=>WebUtility.HtmlDecode(rendered.ToHtmlString());
   async Task Save(string name){if(output is not null)await File.WriteAllTextAsync(Path.Combine(output,name+".html"),rendered.ToHtmlString());}
   Check(Html().Contains("ناحیهٔ کاربری")&&Html().Contains("عملکرد من")&&Html().Contains("تنظیمات امنیتی"),"account hero and performance/security tabs render");
   Check(Html().Contains("activity-log")&&Html().Contains("کد بلند")&&!Html().Contains("<table"),"performance records use compact cards instead of a wide table");
   Check(!Html().Contains("دستمزد من"),"disabled personal payroll remains hidden");await Save("performance");
   api.PayrollEnabled=true;await page.Call("Load",1);page.Refresh();Check(Html().Contains("دستمزد من")&&Html().Contains("120,000"),"authorized personal payroll retains totals and lines");await Save("payroll");
   api.PayrollMissing=true;await page.Call("Load",1);page.Refresh();Check(Html().Contains("activity-log")&&!Html().Contains("دستمزد من"),"missing optional payroll endpoint does not hide activity");api.PayrollMissing=false;
   page.Set("_from",new DateTime(2026,10,5));page.Set("_to",new DateTime(2026,10,4));var requests=api.ActivityReads;await page.Call("Apply");page.Refresh();Check(api.ActivityReads==requests&&Html().Contains("تاریخ شروع و پایان معتبر"),"invalid reporting dates do not send a request");
   page.Set("_security",true);page.Refresh();Check(Html().Contains("عکس پروفایل")&&Html().Contains("current-password")&&Html().Contains("new-password")&&!Html().Contains("activity-log"),"security tab separates photo and credential settings from performance");
   Check(Html().Contains("data:image/png;base64"),"stored avatar is fetched with the authenticated API client and rendered");await Save("security");
   page.Set("_userName","profile-new");page.Set("_currentPassword","original-password");page.Set("_newPassword","new-password-123");page.Set("_confirmation","mismatch");await page.Call("SaveCredentialsAsync");Check(api.Credentials.Count==0,"mismatched password confirmation is caught before sending");
   page.Set("_confirmation","new-password-123");api.FailCredentials=true;await page.Call("SaveCredentialsAsync");page.Refresh();Check(Html().Contains("این نام کاربری قبلاً استفاده شده است")&&!auth.LoggedOut,"server duplicate username feedback is displayed without logging out");
   Check((string)page.Get("_currentPassword")! ==""&&(string)page.Get("_newPassword")! =="","password fields are cleared after a submission");await Save("security-error");
   await page.Call("RemoveAvatarAsync");page.Refresh();Check(api.AvatarRemoved&&((OwnProfileDto)page.Get("_profile")!).AvatarVersion is null&&!Html().Contains("data:image/png;base64"),"avatar removal updates the image to initials");
   api.FailCredentials=false;auth.FailLogout=true;page.Set("_currentPassword","original-password");await page.Call("SaveCredentialsAsync");Check(auth.LoggedOut&&nav.Uri.Contains("/login?accountUpdated=1")&&api.Credentials.Last().NewPassword=="","acknowledged username-only save returns to login even if browser sign-out storage fails");
  });
 }
 private static void Check(bool value,string message){if(!value)throw new Exception(message);Console.WriteLine("PASS "+message);}
}
public sealed class ProfilePreview:MyActivity
{
 public static ProfilePreview? Instance;
 protected override async Task OnInitializedAsync(){await base.OnInitializedAsync();Instance=this;}
 public void Refresh()=>StateHasChanged();
 public object? Get(string name)=>typeof(MyActivity).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(this);
 public void Set(string name,object value)=>typeof(MyActivity).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(this,value);
 public async Task Call(string name,params object[] args){var result=typeof(MyActivity).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(this,args);if(result is Task task)await task;}
}
sealed class ProfileNavigation:NavigationManager{public ProfileNavigation(){Initialize("https://fixture.invalid/","https://fixture.invalid/my-account");}protected override void NavigateToCore(string uri,bool forceLoad){Uri=ToAbsoluteUri(uri).ToString();}}
sealed class ProfileState:AuthenticationStateProvider{public override Task<AuthenticationState> GetAuthenticationStateAsync()=>Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role,"Tuner")],"fixture"))));}
sealed class ProfileJs:IJSRuntime{public ValueTask<T> InvokeAsync<T>(string id,object?[]? args)=>ValueTask.FromResult(default(T)!);public ValueTask<T> InvokeAsync<T>(string id,CancellationToken ct,object?[]? args)=>InvokeAsync<T>(id,args);}
sealed class ProfileAuth:IAuthService
{
 public bool LoggedOut,FailLogout;public bool IsAuthenticated=>!LoggedOut;public string? Token=>null;public string? UserName=>"profile-me";public string? FullName=>"عضو اول";public IReadOnlyList<string> Roles=>["Tuner"];
 public Task<LoginResult> LoginAsync(LoginCommand command)=>Task.FromResult(new LoginResult());public Task LogoutAsync(){LoggedOut=true;if(FailLogout)throw new Exception("Storage temporarily unavailable");return Task.CompletedTask;}
}
sealed class ProfileFixtureApi:HttpMessageHandler
{
 public static readonly Guid UserId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");public static readonly Guid ImageVersion=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
 public bool PayrollEnabled,PayrollMissing,FailCredentials,AvatarRemoved;public int ActivityReads;public List<OwnCredentialsDto> Credentials=[];
 protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
 {
  var path=request.RequestUri!.AbsolutePath;
  if(path.EndsWith("/me/profile"))return Json(new OwnProfileDto{Id=UserId,UserName="profile-me",FullName="رضا ضرغامی",Title="تیونر",AvatarVersion=ImageVersion});
  if(path.EndsWith("/me/activity")){ActivityReads++;return Json(new MyActivityDto{From=new(2026,9,23),To=new(2026,10,5),Total=2,Completed=2,Summary=[new(){Operation="تیون کاسه رو",Count=2}],Items=[new(){Id=Guid.NewGuid(),EventDate=DateTime.UtcNow,Code="کد بلند با جزئیات برای بررسی نمایش روی گوشی 123-456",Operation="تیون کاسه رو",Duration="۲۰ دقیقه",Result="تکمیل‌شده",Details="جزئیات طولانی فعالیت ثبت‌شده در کارگاه برای بررسی شکستن متن در صفحهٔ گوشی."}]});}
  if(path.EndsWith("/activity/payroll")&&PayrollMissing)return new(HttpStatusCode.NotFound);
  if(path.EndsWith("/activity/payroll"))return Json(new MyPayrollDto{Enabled=PayrollEnabled,Total=120000,Lines=[new(){Operation="تیون",Description="متریال و اسکیل ساز",Count=2,Rate=60000,Total=120000}]});
  if(path.EndsWith("/credentials")){Credentials.Add((await request.Content!.ReadFromJsonAsync<OwnCredentialsDto>(ct))!);if(FailCredentials)return new(HttpStatusCode.Conflict){Content=JsonContent.Create(new{Message="این نام کاربری قبلاً استفاده شده است."})};return Json(new OwnCredentialsResult{Changed=true});}
  if(path.EndsWith("/me/profile/avatar")){AvatarRemoved=true;return Json(new OwnAvatarResult());}
  if(path.EndsWith("/avatar"))return new(HttpStatusCode.OK){Content=new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="))};
  if(path.EndsWith("/unread"))return Json(new UnreadDto());
  throw new Exception("Unexpected fixture API request "+path);
 }
 private static HttpResponseMessage Json(object value)=>new(HttpStatusCode.OK){Content=JsonContent.Create(value)};
}
