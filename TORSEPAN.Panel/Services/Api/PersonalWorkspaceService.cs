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
    public async Task<InboxDto> InboxAsync(int page=1)=>await api.GetAsync<InboxDto>($"notifications?page={page}")??new();
    public async Task ReadAsync(Guid id)
    { await api.PostAsync<object,object?>($"notifications/{id}/read",new{});await RefreshUnreadAsync(); }
    public async Task<List<MessageRecipientDto>> RecipientsAsync()=>await api.GetAsync<List<MessageRecipientDto>>("notifications/recipients")??[];
    public async Task SendAsync(Guid id,string title,string body,bool broadcast,Guid? recipient)
    { await api.PostAsync<object,object?>("notifications",new{Id=id,Title=title,Body=body,Broadcast=broadcast,RecipientId=recipient});await RefreshUnreadAsync(); }
}

