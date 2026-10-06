using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using TORSEPAN.Application.Auth.Commands.DeleteUser;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Panel.Models;
using TORSEPAN.Panel.Services;
using TORSEPAN.Panel.Services.Api;
using TORSEPAN.Panel.Services.Auth;

static void Check(bool ok, string label) { if (!ok) throw new Exception(label); Console.WriteLine("PASS " + label); }
var admin = new User("admin", "مدیر"); admin.SetRole("Administrator");
var member = new User("member", "عضو"); member.SetPassword("member-password"); member.ChangeAvatar([1, 2, 3]);
var token = new RefreshToken(member.Id, "fixture-refresh", DateTime.UtcNow.AddDays(1), member.CredentialVersion); member.RefreshTokens.Add(token);
var production = new ProductionEvent(null, null, null, member.Id, TORSEPAN.Domain.Enums.ProductionAction.Design, TORSEPAN.Domain.Enums.EventResult.Completed, null, "historical work");
var users = new MemoryUsers([admin, member]); var unit = new MemoryUnit(users); var delete = new DeleteUserCommandHandler(unit);
async Task Reject(DeleteUserCommand command, string label)
{
    try { await delete.Handle(command, default); throw new Exception("accepted: " + label); }
    catch (InvalidOperationException) { Check(true, label); }
}
await Reject(new(member.Id, member.Id), "self deletion is rejected");
await Reject(new(admin.Id, member.Id), "last active administrator is protected");
var oldVersion = member.CredentialVersion;
await delete.Handle(new(member.Id, admin.Id), default);
Check(member.IsDeleted && !member.IsActive && member.CredentialVersion > oldVersion, "account deletion disables all existing access versions");
Check(token.Revoked && !member.VerifyPassword("member-password") && member.AvatarPng is null, "refresh sessions, password and avatar are removed");
Check(member.FullName == "عضو" && production.UserId == member.Id && production.Description == "historical work", "production identity and historical name remain intact");
Check(!(await users.GetAllAsync()).Any(x => x.Id == member.Id) && await users.GetByIdAsync(member.Id) is null, "deleted account is absent from management and editing");
member.Activate(); Check(!member.IsActive, "archived accounts cannot be reactivated");
Check(await users.GetByUsernameAsync("member") is null && member.UserName.StartsWith("deleted-"), "previous login name is released without erasing historical records");
var secondAdmin = new User("admin2", "مدیر دوم"); secondAdmin.SetRole("Administrator"); users.Items.Add(secondAdmin);
await delete.Handle(new(admin.Id, secondAdmin.Id), default);
Check(admin.IsDeleted && secondAdmin.IsActive, "an administrator may be deleted when another active administrator remains");

var fixture = new StatusApi(); var api = new ApiClient(new HttpClient(fixture) { BaseAddress = new("https://fixture.invalid/api/") }, new TokenStorage(new FixtureJs()));
var workspace = new PersonalWorkspaceService(api); var arrivals = new List<(Guid,long)>(); workspace.IncomingMessagesChanged += (id,total) => arrivals.Add((id,total));
var first = Guid.NewGuid(); var other = Guid.NewGuid();
async Task Status(Guid id, long? total, int unread) { fixture.Status = new() { UserId=id, TotalIncoming=total, Count=unread }; await workspace.RefreshUnreadAsync(); }
await Status(first, 100, 3); Check(arrivals.Count == 0, "existing messages on initial login stay silent");
await Status(first, 101, 0); Check(arrivals.Count == 1, "new arrivals chime even when the chat immediately marks them read");
await Status(first, 101, 0); await Status(first, 101, 8); Check(arrivals.Count == 1, "same arrival total and unread-count changes do not chime");
fixture.Fail = true; await workspace.RefreshUnreadAsync(); fixture.Fail = false;
await Status(first, 102, 1); Check(arrivals.Count == 2 && workspace.UnreadCount == 1, "temporary API errors do not reset the arrival baseline");
await Status(other, 800, 20); Check(arrivals.Count == 2, "switching accounts does not chime for old messages");
await Status(other, 801, 21); Check(arrivals.Count == 3 && arrivals[^1].Item1 == other, "the new account receives its own new-message event");
await Status(Guid.Empty, null, 22); Check(arrivals.Count == 3, "an older API response stays compatible without false sound events");
Console.WriteLine("Account and messaging smoke passed.");

sealed class StatusApi : HttpMessageHandler
{
    public UnreadDto Status = new(); public bool Fail;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
        Task.FromResult(new HttpResponseMessage(Fail ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK) { Content = JsonContent.Create(Status) });
}
sealed class FixtureJs : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string id, object?[]? args) => ValueTask.FromResult(default(TValue)!);
    public ValueTask<TValue> InvokeAsync<TValue>(string id, CancellationToken ct, object?[]? args) => InvokeAsync<TValue>(id,args);
}
sealed class MemoryUsers(List<User> items) : IUserRepository
{
    public List<User> Items = items;
    public Task<User?> GetByIdAsync(Guid id) => Task.FromResult(Items.FirstOrDefault(u => u.Id==id && !u.IsDeleted));
    public Task<User?> GetByUsernameAsync(string name) => Task.FromResult(Items.FirstOrDefault(u => !u.IsDeleted && u.UserName.Equals(name,StringComparison.OrdinalIgnoreCase)));
    public Task<List<User>> GetAllAsync() => Task.FromResult(Items.Where(u=>!u.IsDeleted).ToList());
    async Task<IEnumerable<User>> IRepository<User>.GetAllAsync() => await GetAllAsync();
    public Task<IEnumerable<User>> FindAsync(Expression<Func<User,bool>> expression) => Task.FromResult(Items.Where(expression.Compile()));
    public Task<bool> ExistsAsync(Guid id) => Task.FromResult(Items.Any(u=>u.Id==id && !u.IsDeleted));
    public Task AddAsync(User user) { Items.Add(user); return Task.CompletedTask; }
    public void Update(User user) { }
    public void Remove(User user) => throw new Exception("Physical deletion must never erase production/message references.");
}
sealed class MemoryUnit(IUserRepository users) : IUnitOfWork
{
    public IUserRepository Users => users;
    public IRoleRepository Roles => null!; public IUserRoleRepository UserRoles => null!; public IRefreshTokenRepository RefreshTokens => null!;
    public IHandpanRepository Handpans => null!; public IHandpanAssemblyRepository HandpanAssemblies => null!; public IBowlRepository Bowls => null!;
    public IMaterialRepository Materials => null!; public IScaleRepository Scales => null!; public IProductionEventRepository ProductionEvents => null!;
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
}
