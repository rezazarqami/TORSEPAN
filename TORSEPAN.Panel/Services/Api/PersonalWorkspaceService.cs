using System.Globalization;
using TORSEPAN.Panel.Models;
namespace TORSEPAN.Panel.Services.Api;
public sealed class PersonalWorkspaceService(ApiClient api)
{
    public event Action? UnreadChanged;
    public int? UnreadCount {get;private set;}
    public async Task RefreshUnreadAsync()
    {
        try { UnreadCount=(await api.GetAsync<UnreadDto>("notifications/unread"))?.Count; }
        catch { UnreadCount=null; }
        UnreadChanged?.Invoke();
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
