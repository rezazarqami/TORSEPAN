using System.Net;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using TORSEPAN.Application.Auth.Commands.CreateUser;
using TORSEPAN.Application.Auth.Commands.UpdateUser;
using TORSEPAN.Application.Auth.Queries.GetRoles;
using TORSEPAN.Application.Auth.Queries.GetUsers;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Panel.Components.Pages;
using TORSEPAN.Panel.Components.Shared;
using TORSEPAN.Panel.Services;
using TORSEPAN.Panel.Services.Api;
using TORSEPAN.Panel.Services.Auth;
using Details = TORSEPAN.Application.Auth.Queries.GetUserById.UserDto;

static void Check(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS "+label);}
var member=new User("member","عضو");member.SetDisplayOrder(8);var role=new Role("Tuner","تیونر");
var users=Proxy<IUserRepository>.Make((m,a)=>m=="GetByIdAsync"?Task.FromResult<User?>(member):m=="GetByUsernameAsync"?Task.FromResult<User?>(member):null);
var roles=Proxy<IRoleRepository>.Make((m,a)=>m=="GetByIdAsync"?Task.FromResult<Role?>(role):null);
var userRoles=Proxy<IUserRoleRepository>.Make((m,a)=>Task.CompletedTask);
var unit=Proxy<IUnitOfWork>.Make((m,a)=>m switch {"get_Users"=>users,"get_Roles"=>roles,"get_UserRoles"=>userRoles,"SaveChangesAsync"=>Task.FromResult(1),_=>null});
var handler=new UpdateUserCommandHandler(unit);
var command=new UpdateUserCommand(member.Id,"member","عضو","تیونر",true,[role.Id],3);
await handler.Handle(command,default);Check(member.DisplayOrder==3,"editing one member persists their display order");
await handler.Handle(command with {DisplayOrder=null},default);Check(member.DisplayOrder==3,"older clients preserve order when omitted");
try{await handler.Handle(command with {DisplayOrder=-1,FullName="changed"},default);throw new Exception("negative accepted");}catch(InvalidOperationException){Check(member.DisplayOrder==3&&member.FullName=="عضو","invalid order rejects before changing profile");}
var details=await new TORSEPAN.Application.Auth.Queries.GetUserById.GetUserByIdQueryHandler(unit).Handle(new(member.Id),default);
Check(details.DisplayOrder==3,"edit details return saved order");

var fixture=new FixtureUsers();var services=new ServiceCollection();services.AddLogging();services.AddAuthorizationCore();
services.AddSingleton<IUserService>(fixture);services.AddSingleton<AuthenticationStateProvider>(new FixtureAuth(fixture.Items[0].Id));
services.AddSingleton<NavigationManager,FixtureNavigation>();services.AddSingleton<IJSRuntime,FixtureJs>();
services.AddSingleton(new HttpClient{BaseAddress=new("https://fixture.invalid/")});services.AddScoped<TokenStorage>();services.AddScoped<ApiClient>();services.AddScoped<PersonalWorkspaceService>();services.AddScoped<PayrollExclusionService>();
await using var provider=services.BuildServiceProvider();await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
var output=Environment.GetEnvironmentVariable("USERS_PREVIEW_DIR");if(output is not null)Directory.CreateDirectory(output);
await renderer.Dispatcher.InvokeAsync(async()=>{
 var root=await renderer.RenderComponentAsync<UsersPreview>(ParameterView.Empty);var view=UsersPreview.Instance!;
 string Html()=>WebUtility.HtmlDecode(root.ToHtmlString());
 Check(Html().Contains("مدیریت کاربران")&&Html().Contains("علی شاطریان")&&!Html().Contains("ثبت ترتیب نمایش"),"real user rows replace table and standalone order panel");
 if(output is not null)await File.WriteAllTextAsync(Path.Combine(output,"users.html"),root.ToHtmlString());
 view.Set("_search","علي");view.Refresh();Check(Html().Contains("علی شاطریان")&&!Html().Contains("رضا ضرغامی"),"Persian name search normalizes Arabic yeh");
 view.Set("_search","");view.Set("_role","Final Tuner");view.Refresh();Check(Html().Contains("رضا ضرغامی")&&!Html().Contains("علی شاطریان"),"role filter applies to actual roles");
 view.Set("_role","");fixture.FailStatus=true;await view.Call("ToggleActiveAsync",fixture.Items[0]);view.Refresh();Check(fixture.Items[0].IsActive&&Html().Contains("تغییر وضعیت ذخیره نشد"),"failed activation leaves visible status intact");
 fixture.FailStatus=false;await view.Call("ToggleActiveAsync",fixture.Items[0]);view.Refresh();Check(!fixture.Items[0].IsActive,"acknowledged activation updates status");
 await view.Call("AskDelete",fixture.Items[1]);view.Refresh();Check(Html().Contains("حذف حساب کاربر"),"delete still requires confirmation");
 await view.Call("CancelDelete");
 view.Set("_loadError","fixture");await view.Call("LoadAsync");Check(view.Get("_loadError") is null,"retry clears the previous load error");
});
await renderer.Dispatcher.InvokeAsync(async()=>{
 var root=await renderer.RenderComponentAsync<EditPreview>(ParameterView.FromDictionary(new Dictionary<string,object?>{{"UserId",fixture.Items[0].Id}}));var view=EditPreview.Instance!;
 Check(WebUtility.HtmlDecode(root.ToHtmlString()).Contains("ترتیب نمایش در کارگاه"),"order is inside member edit form");
 var model=view.Get("_model")!;model.GetType().GetProperty("DisplayOrder")!.SetValue(model,6);await view.Call("Save");Check(fixture.Saved?.DisplayOrder==6,"profile save includes the selected member order");
});
await renderer.Dispatcher.InvokeAsync(async()=>{
 var root=await renderer.RenderComponentAsync<PayrollExclusions>(ParameterView.Empty);
 if(output is not null)await File.WriteAllTextAsync(Path.Combine(output,"exclusions-header.html"),root.ToHtmlString());
});
Console.WriteLine("User management smoke passed.");

public class Proxy<T>:DispatchProxy where T:class{public Func<string,object?[]?,object?> Handle=null!;public static T Make(Func<string,object?[]?,object?> h){var v=Create<T,Proxy<T>>();((Proxy<T>)(object)v).Handle=h;return v;}protected override object? Invoke(MethodInfo? m,object?[]? args)=>Handle(m!.Name,args);}
sealed class UsersPreview:UsersList{public static UsersPreview? Instance;protected override async Task OnInitializedAsync(){Instance=this;await base.OnInitializedAsync();}public void Set(string n,object? v)=>typeof(UsersList).GetField(n,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(this,v);public object? Get(string n)=>typeof(UsersList).GetField(n,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(this);public async Task Call(string n,params object?[] args){if(typeof(UsersList).GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(this,args) is Task t)await t;}public void Refresh()=>StateHasChanged();}
sealed class EditPreview:EditUser{public static EditPreview? Instance;protected override async Task OnParametersSetAsync(){Instance=this;await base.OnParametersSetAsync();}public object? Get(string n)=>typeof(EditUser).GetField(n,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(this);public async Task Call(string n){if(typeof(EditUser).GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(this,null) is Task t)await t;}}
sealed class FixtureUsers:IUserService{
 public List<UserDto> Items=[new(){Id=Guid.NewGuid(),FullName="رضا ضرغامی",UserName="reza",Title="مدیر تولید",Roles=["Final Tuner","مدیر تولید","انبار","تیونر","دیمپل","سالن کار","شیپر","کنترل کیفیت"],IsActive=true},new(){Id=Guid.NewGuid(),FullName="علی شاطریان",UserName="alishaterian",Title="مدیر سیستم",Roles=["مدیر سالن"],IsActive=true},new(){Id=Guid.NewGuid(),FullName="شاهین نجفی",UserName="shahin",Title="شیپر",Roles=["Shaper"],IsActive=false},new(){Id=Guid.NewGuid(),FullName="نیما رضایی",UserName="nima",Roles=["انبار"],IsActive=true}];public bool FailStatus;public UpdateUserCommand? Saved;readonly Guid roleId=Guid.NewGuid();
 public Task<List<UserDto>> GetUsersAsync()=>Task.FromResult(Items);public Task<Guid> CreateUserAsync(CreateUserCommand c)=>throw new NotSupportedException();public Task<List<RoleDto>> GetRolesAsync()=>Task.FromResult(new List<RoleDto>{new(){Id=roleId,DisplayName="تیونر"}});public Task<Details?> GetUserAsync(Guid id)=>Task.FromResult<Details?>(new(){Id=id,UserName="reza",FullName="رضا ضرغامی",RoleIds=[roleId],DisplayOrder=3});public Task UpdateUserAsync(UpdateUserCommand c){Saved=c;return Task.CompletedTask;}public Task DeleteUserAsync(Guid id)=>Task.CompletedTask;public Task SetActiveAsync(Guid id,bool active)=>FailStatus?Task.FromException(new HttpRequestException()):Task.CompletedTask;
}
sealed class FixtureAuth(Guid id):AuthenticationStateProvider{public override Task<AuthenticationState> GetAuthenticationStateAsync()=>Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,id.ToString()),new Claim(ClaimTypes.Role,"Administrator")},"fixture"))));}
sealed class FixtureJs:IJSRuntime{public ValueTask<T> InvokeAsync<T>(string id,object?[]? args)=>ValueTask.FromResult(default(T)!);public ValueTask<T> InvokeAsync<T>(string id,CancellationToken ct,object?[]? args)=>InvokeAsync<T>(id,args);}
sealed class FixtureNavigation:NavigationManager{public FixtureNavigation(){Initialize("https://fixture.invalid/","https://fixture.invalid/users/list");}protected override void NavigateToCore(string uri,bool forceLoad){}}
