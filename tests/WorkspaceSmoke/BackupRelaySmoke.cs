using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TORSEPAN.Panel.Services;

internal static class BackupRelaySmoke
{
    public static async Task RunAsync()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["TelegramRelay:Secret"]="fixture-secret", ["TelegramRelay:BotToken"]="fixture-private-token",
            ["TelegramRelay:ChatId"]="fixture-chat"
        }).Build();
        var calls = 0;
        var reply = "{\"ok\":true}";
        var fail = false;
        using var telegram = new HttpClient(new Handler(async (request, ct) => {
            calls++;
            var form = (MultipartFormDataContent)request.Content!;
            Check(form.Any(x=>x.Headers.ContentDisposition?.Name?.Trim('"')=="document"), "relay forwards a Telegram document");
            await form.CopyToAsync(Stream.Null, ct);
            if (fail) throw new HttpRequestException("https://api.telegram.org/botfixture-private-token/failed");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content=new StringContent(reply) };
        }));
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var app = builder.Build();
        app.MapPost("/default", (HttpRequest request, CancellationToken ct) => DatabaseBackupRelay.ForwardAsync(request, config, telegram, ct));
        app.MapPost("/backup", (HttpRequest request, CancellationToken ct) => DatabaseBackupRelay.ForwardAsync(request, config, telegram, ct))
            .WithMetadata(new RequestSizeLimitAttribute(DatabaseBackupRelay.MaxRequestBytes));
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        async Task<(HttpStatusCode Code,string Body)> Send(string path, int size=64, string secret="fixture-secret")
        {
            using var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(new byte[size]), "backup", "fixture.dump");
            using var request = new HttpRequestMessage(HttpMethod.Post,path) { Content=form };
            request.Headers.Add("X-Relay-Secret",secret);
            using var response = await client.SendAsync(request);
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }
        var result = await Send("/backup",secret:"wrong");
        Check(result.Code==HttpStatusCode.Unauthorized && calls==0,"backup authentication is checked before Telegram delivery");
        try
        {
            result = await Send("/default",31_000_000);
            Check((int)result.Code==413 && calls==0,"reproduces rejection of a legacy backup part above Kestrel's default limit");
        }
        catch (HttpRequestException error) when (error.Message.Contains("copying content to a stream"))
        {
            Check(calls==0,"default upload limit reproduces the live stream-copy failure before Telegram is called");
        }
        result = await Send("/backup",31_000_000);
        Check(result.Code==HttpStatusCode.OK && result.Body.Contains("sent") && calls==1,"bounded backup route accepts legacy large parts and returns a delivery receipt");
        reply="{\"ok\":false}";
        Check((await Send("/backup")).Code==HttpStatusCode.BadGateway,"Telegram rejection is never recorded as backup success");
        reply="<html>not a receipt</html>";
        Check((await Send("/backup")).Code==HttpStatusCode.BadGateway,"invalid Telegram receipt is not success");
        fail=true;
        result=await Send("/backup");
        Check(result.Code==HttpStatusCode.BadGateway && !result.Body.Contains("fixture-private-token"),"transport errors are reported without exposing the bot token");
        await app.StopAsync();
    }
    private static void Check(bool value,string message){if(!value)throw new Exception(message);Console.WriteLine("PASS "+message);}
    private sealed class Handler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> send):HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)=>send(request,ct); }
}
