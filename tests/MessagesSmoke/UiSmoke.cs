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
using TORSEPAN.API.Controllers;
using TORSEPAN.Panel.Components.Pages;
using TORSEPAN.Panel.Models;
using TORSEPAN.Panel.Services;
using TORSEPAN.Panel.Services.Api;
using TORSEPAN.Panel.Services.Auth;

internal static class UiSmoke
{
 public static async Task RunAsync()
 {
  var output=Environment.GetEnvironmentVariable("MESSAGES_PREVIEW_DIR");if(output is not null)Directory.CreateDirectory(output);
  foreach(var role in new[]{"Tuner","ProductionManager","Administrator"})
  {
   var api=new MessageFixtureApi();var js=new MessageFixtureJs();var services=new ServiceCollection();
   services.AddLogging();services.AddAuthorizationCore();services.AddCascadingAuthenticationState();services.AddSingleton<AuthenticationStateProvider>(new MessageFixtureAuth(role));services.AddSingleton<IJSRuntime>(js);services.AddScoped<TokenStorage>();
   services.AddSingleton(new HttpClient(api){BaseAddress=new Uri("https://fixture.invalid/api/")});services.AddScoped<ApiClient>();services.AddScoped<PersonalWorkspaceService>();
   await using var provider=services.BuildServiceProvider();await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
   await renderer.Dispatcher.InvokeAsync(async()=>
   {
    var rendered=await renderer.RenderComponentAsync<MessagePreview>(ParameterView.Empty);var page=MessagePreview.Instance!;
    string Html()=>WebUtility.HtmlDecode(rendered.ToHtmlString());
    async Task Save(string name){if(output is not null&&role=="ProductionManager")await File.WriteAllTextAsync(Path.Combine(output,name+".html"),rendered.ToHtmlString());}
    var html=Html();
    Check(html.Contains("WORKSHOP · MESSAGES")&&html.Contains("مدیریت پیام‌ها")&&!html.Contains("داشبورد")&&!html.Contains("پیام‌های من"),role+" sees the redesigned message center without old headings and dashboard action");
    Check(html.Contains("اطلاعیه‌های کارگاه")&&html.Contains("چت و گفتگو")&&!html.Contains("data-incoming-id"),role+" announcement and chat tabs keep private messages separate");await Save("announcements");
    await page.Call("SwitchTab",true);page.Refresh();html=Html();
    Check(html.Contains("عضو ۹")&&html.Contains("contact-grid"),role+" has one conversation per other member");
    Check(html.Contains("ارسال به اطلاعیه‌های کارگاه")== (role!="Tuner"),role+" announcement publishing entry follows management permissions");await Save("contacts");
    var member=((List<ConversationDto>)page.Get("_members")!)[0];await page.Call("OpenChatAsync",member);page.Refresh();html=Html();
    Check(html.Contains("پیام دریافت‌شده")&&html.Contains("ارسال‌شده")&&html.Contains("خوانده‌شده")&&html.Contains("✓✓"),role+" chat archive shows incoming messages and single/double status ticks");
    Check(api.ReadIds.Count==0,role+" opening a conversation does not mark hidden history as read");
    js.Visible=false;js.VisibleIds=[MessageFixtureApi.IncomingId.ToString()];await page.Call("ReadVisibleAsync");Check(api.ReadIds.Count==0,"hidden document never marks messages read");
    js.Visible=true;js.VisibleIds=[];await page.Call("ReadVisibleAsync");Check(api.ReadIds.Count==0,"offscreen messages never receive read receipts");
    js.VisibleIds=[MessageFixtureApi.IncomingId.ToString()];await page.Call("ReadVisibleAsync");
    Check(api.ReadIds.SequenceEqual([MessageFixtureApi.IncomingId]),role+" only displayed incoming message IDs are submitted for reading");
    await Save("chat");
    var draft=page.GetProperty("Draft");SetDraft(draft,"Body","پاسخ همکار");api.FailSend=true;
    await page.Call("SendAsync");page.Refresh();Check((string)draft.GetType().GetProperty("Body")!.GetValue(draft)! =="پاسخ همکار"&&Html().Contains("متن محفوظ است"),role+" failed send preserves the draft and shows retry guidance");
    api.FailSend=false;await page.Call("SendAsync");page.Refresh();
    Check(api.Sends.Count==2&&api.Sends[0].Id==api.Sends[1].Id&&api.Sends[1].RecipientId==member.Id&&!api.Sends[1].Broadcast,role+" retry preserves the send ID and exact private recipient");
    Check((string)draft.GetType().GetProperty("Body")!.GetValue(draft)! =="",role+" acknowledged send clears the draft");
    api.Messages.Single(x=>x.Id==MessageFixtureApi.OutgoingId).ReadAt=DateTime.UtcNow;
    await page.Call("RefreshAsync",false);page.Refresh();
    Check(((List<ChatMessageDto>)page.Get("_messages")!).Single(x=>x.Id==MessageFixtureApi.OutgoingId).ReadAt.HasValue,role+" refresh updates persisted outgoing read receipts");
    SetDraft(page.GetProperty("Draft"),"Body","مسوده اول");await page.Call("OpenChatAsync",((List<ConversationDto>)page.Get("_members")!)[1]);SetDraft(page.GetProperty("Draft"),"Body","مسوده دوم");await page.Call("OpenChatAsync",member);
    Check((string)page.GetProperty("Draft").GetType().GetProperty("Body")!.GetValue(page.GetProperty("Draft"))! =="مسوده اول","switching peers preserves separate drafts");
    SetDraft(page.GetProperty("Draft"),"Body","ارسال نامطمئن اول");api.FailSend=true;await page.Call("SendAsync");var firstPending=api.Sends.Last().Id;
    await page.Call("OpenChatAsync",((List<ConversationDto>)page.Get("_members")!)[1]);SetDraft(page.GetProperty("Draft"),"Body","ارسال به همکار دیگر");api.FailSend=false;await page.Call("SendAsync");
    await page.Call("OpenChatAsync",member);await page.Call("SendAsync");
    Check(api.Sends.Last().Id==firstPending,"retry identity survives switching conversations and sending elsewhere");
    if(role!="Tuner")
    {
     await page.Call("OpenBroadcast");page.Refresh();await Save("broadcast");var announcement=page.GetProperty("Draft");SetDraft(announcement,"Title","اطلاعیه تازه");SetDraft(announcement,"Body","برنامه جدید کارگاه");
     await page.Call("SendAsync");page.Refresh();Check(api.Sends.Last().Broadcast&&api.Sends.Last().RecipientId is null,"announcement composer sends to the whole workshop channel");
    }
    page.Dispose();
   });
  }
 }
 private static void SetDraft(object draft,string name,string value)=>draft.GetType().GetProperty(name)!.SetValue(draft,value);
 private static void Check(bool value,string message){if(!value)throw new Exception(message);Console.WriteLine("PASS "+message);}
}
public sealed class MessagePreview:Notifications
{
 public static MessagePreview? Instance;
 protected override async Task OnInitializedAsync(){await base.OnInitializedAsync();Instance=this;}
 public void Refresh()=>StateHasChanged();
 public object? Get(string name)=>typeof(Notifications).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(this);
 public object GetProperty(string name)=>typeof(Notifications).GetProperty(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(this)!;
 public async Task Call(string name,params object[] args){var result=typeof(Notifications).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(this,args);if(result is Task task)await task;}
}
sealed class MessageFixtureAuth(string role):AuthenticationStateProvider
{
 public override Task<AuthenticationState> GetAuthenticationStateAsync()=>Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role,role)],"fixture"))));
}
sealed class MessageFixtureJs:IJSRuntime
{
 public bool Visible=true;public string[] VisibleIds=[];
 public ValueTask<T> InvokeAsync<T>(string id,object?[]? args)
 {
  object? value=id switch{"workshopMessages.isVisible"=>Visible,"workshopMessages.visibleIncoming"=>VisibleIds,"workshopMessages.atBottom"=>true,_=>default(T)};
  return ValueTask.FromResult(value is null?default(T)!:(T)value);
 }
 public ValueTask<T> InvokeAsync<T>(string id,CancellationToken ct,object?[]? args)=>InvokeAsync<T>(id,args);
}
sealed class MessageFixtureApi:HttpMessageHandler
{
 public static readonly Guid IncomingId=Guid.Parse("11111111-1111-1111-1111-111111111111"),OutgoingId=Guid.Parse("22222222-2222-2222-2222-222222222222");
 public readonly List<ChatMessageDto> Messages=[new(){Id=IncomingId,Body="پیام دریافت‌شده\nبرای گفتگوی کارگاه",CreatedAt=DateTime.UtcNow.AddMinutes(-5)},new(){Id=OutgoingId,Body="پیام ارسال‌شده",IsMine=true,CreatedAt=DateTime.UtcNow.AddMinutes(-4)},new(){Id=Guid.NewGuid(),Body="پیام قبلی خوانده شده",IsMine=true,ReadAt=DateTime.UtcNow.AddMinutes(-1),CreatedAt=DateTime.UtcNow.AddMinutes(-3)}];
 public readonly List<SendWorkshopMessage> Sends=[];public readonly List<Guid> ReadIds=[];public bool FailSend;
 protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
 {
  var path=request.RequestUri!.AbsolutePath;object data=new{};
  if(path=="/api/notifications/announcements")data=new InboxDto{Total=1,Items=[new(){Id=Guid.NewGuid(),Title="برنامه کارگاه",Body="برنامه این هفته در کارگاه\nپیام‌های قبلی در آرشیو محفوظ هستند.",SenderName="رضا ضرغامی",CreatedAt=DateTime.UtcNow.AddDays(-1)}]};
  else if(path=="/api/notifications/conversations")data=new ConversationListDto{Items=Enumerable.Range(1,9).Select(i=>new ConversationDto{Id=Guid.Parse($"00000000-0000-0000-0000-{i:000000000000}"),Name="عضو "+new[]{"۱","۲","۳","۴","۵","۶","۷","۸","۹"}[i-1],LastBody=i==1?"پیام دریافت‌شده":null,UnreadCount=i==1?1:0}).ToList()};
  else if(path=="/api/notifications/unread")data=new UnreadDto{Count=1};
  else if(path.EndsWith("/read"))
  {
   var input=await request.Content!.ReadFromJsonAsync<ReadChatMessages>(ct);ReadIds.AddRange(input!.Ids!);var now=DateTime.UtcNow;foreach(var m in Messages.Where(x=>input.Ids!.Contains(x.Id)))m.ReadAt=now;data=new ChatReadDto{Changed=input.Ids!.Count,ReadAt=now};
  }
  else if(path.Contains("/conversations/"))data=new ChatHistoryDto{Total=Messages.Count,Items=Messages.ToList()};
  else if(path=="/api/notifications"&&request.Method==HttpMethod.Post)
  {
   var input=(await request.Content!.ReadFromJsonAsync<SendWorkshopMessage>(ct))!;Sends.Add(input);if(FailSend)return new(HttpStatusCode.InternalServerError);
   if(!input.Broadcast&&!Messages.Any(x=>x.Id==input.Id))Messages.Add(new(){Id=input.Id,Body=input.Body!,Title=input.Title!,IsMine=true,CreatedAt=DateTime.UtcNow});
  }
  else throw new Exception("Unexpected messaging fixture request "+path);
  return new(HttpStatusCode.OK){Content=JsonContent.Create(data)};
 }
}
