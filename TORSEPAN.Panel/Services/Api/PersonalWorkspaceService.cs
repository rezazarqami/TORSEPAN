using System.Globalization;
using System.Text.Json;
using Microsoft.JSInterop;
using TORSEPAN.Panel.Models;
namespace TORSEPAN.Panel.Services.Api;
public sealed class PersonalWorkspaceService(ApiClient api)
{
    public async Task<string> ConfigureAlertsAsync(IJSRuntime js, object? callback = null)
    {
        var settings = await api.GetAsync<MessagePushKeyDto>("notifications/push/key") ?? throw new InvalidOperationException();
        var result = await js.InvokeAsync<JsonElement>("workshopMessages.configureAlerts", settings, callback);
        if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("subscription", out var value))
        {
            var subscription = value.Deserialize<MessagePushSubscriptionDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            await SubscribePushAsync(subscription);
            return "اعلان این دستگاه فعال است.";
        }
        return result.ValueKind == JsonValueKind.String ? result.GetString()! : "وضعیت اعلان در دسترس نیست.";
    }
    public async Task SubscribePushAsync(MessagePushSubscriptionDto subscription) =>
        await api.PutAccountAsync<MessagePushSubscriptionDto, object>("notifications/push", subscription);
    public async Task DisablePushAsync(IJSRuntime js)
    {
        var subscription = await js.InvokeAsync<MessagePushSubscriptionDto?>("workshopMessages.pushSubscription");
        try { if (subscription is not null) await api.PostAsync<object, object?>("notifications/push/unsubscribe", new { subscription.Endpoint }); }
        finally { await js.InvokeVoidAsync("workshopMessages.clearAlerts"); }
    }
    public event Action? UnreadChanged;
    public int? UnreadCount {get;private set;}
    public event Action<Guid, long>? IncomingMessagesChanged;
    private readonly SemaphoreSlim _unreadLock = new(1, 1);
    private Guid _incomingUser;
    private long? _totalIncoming;
    public async Task RefreshUnreadAsync()
    {
        Guid user = Guid.Empty; long? incoming = null;
        await _unreadLock.WaitAsync();
        try
        {
            var status = await api.GetAsync<UnreadDto>("notifications/unread");
            UnreadCount = status?.Count;
            if (status is { UserId: var id, TotalIncoming: long total } && id != Guid.Empty)
            {
                if (_incomingUser == id && _totalIncoming.HasValue && total > _totalIncoming)
                { user = id; incoming = total; }
                _incomingUser = id; _totalIncoming = total;
            }
        }
        catch { UnreadCount = null; } // Keep the baseline during temporary connection failures.
        finally { _unreadLock.Release(); }
        UnreadChanged?.Invoke();
        if (incoming.HasValue) IncomingMessagesChanged?.Invoke(user, incoming.Value);
    }
    public async Task<MyActivityDto> ActivityAsync(DateTime? from,DateTime? to,int page=1)
    {
        var query=$"me/activity?page={page}";
        if(from.HasValue)query+="&from="+from.Value.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        if(to.HasValue)query+="&to="+to.Value.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        return await api.GetAsync<MyActivityDto>(query)??new();
    }
    public async Task<MyPayrollDto> PayrollAsync(DateTime? from,DateTime? to)
    {
        var query="me/activity/payroll?x=1";
        if(from.HasValue)query+="&from="+from.Value.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        if(to.HasValue)query+="&to="+to.Value.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        return await api.GetAsync<MyPayrollDto>(query)??new();
    }
    public async Task<InboxDto> InboxAsync(int page=1)=>await api.GetAsync<InboxDto>($"notifications?page={page}")??new();
    public async Task<InboxDto> AnnouncementsAsync(int page=1)=>await api.GetAsync<InboxDto>($"notifications/announcements?page={page}")??new();
    public async Task<List<ConversationDto>> ConversationsAsync()=>(await api.GetAsync<ConversationListDto>("notifications/conversations"))?.Items??[];
    public async Task<ChatHistoryDto> ChatAsync(Guid peer,int page=1)=>await api.GetAsync<ChatHistoryDto>($"notifications/conversations/{peer}?page={page}")??new();
    public async Task<ChatReadDto> ReadChatAsync(Guid peer,IEnumerable<Guid> ids)
    {
        var result=await api.PostAsync<object,ChatReadDto>($"notifications/conversations/{peer}/read",new{Ids=ids.ToArray()})??new();
        await RefreshUnreadAsync();return result;
    }
    public async Task ReadAsync(Guid id)
    { await api.PostAsync<object,object?>($"notifications/{id}/read",new{});await RefreshUnreadAsync(); }
    public async Task<List<MessageRecipientDto>> RecipientsAsync()=>await api.GetAsync<List<MessageRecipientDto>>("notifications/recipients")??[];
    public async Task SendAsync(Guid id,string title,string body,bool broadcast,Guid? recipient)
    { await api.PostAsync<object,object?>("notifications",new{Id=id,Title=title,Body=body,Broadcast=broadcast,RecipientId=recipient});await RefreshUnreadAsync(); }
    public Task<OwnProfileDto?> ProfileAsync()=>api.GetAsync<OwnProfileDto>("me/profile");
    public async Task<OwnCredentialsResult> SaveCredentialsAsync(OwnCredentialsDto request)=>await api.PutAccountAsync<OwnCredentialsDto,OwnCredentialsResult>("me/profile/credentials",request)??throw new InvalidOperationException();
    public async Task<OwnAvatarResult> SaveAvatarAsync(byte[]? png)=>await api.PutAccountAsync<object,OwnAvatarResult>("me/profile/avatar",new{PngBase64=png is null?null:Convert.ToBase64String(png)})??throw new InvalidOperationException();
    private readonly Dictionary<Guid,(Guid Version,Task<string?> Image)> _avatars=[];
    public Task<string?> AvatarAsync(Guid id,Guid version)
    {
        if(_avatars.TryGetValue(id,out var cached)&&cached.Version==version)return cached.Image;
        var image=LoadAvatarAsync(id);_avatars[id]=(version,image);return image;
    }
    private async Task<string?> LoadAvatarAsync(Guid id)
    {
        try{return "data:image/png;base64,"+Convert.ToBase64String(await api.GetBytesAsync($"users/{id}/avatar"));}
        catch{_avatars.Remove(id);return null;}
    }
}
