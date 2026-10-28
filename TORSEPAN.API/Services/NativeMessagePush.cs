using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TORSEPAN.Domain.Entities;
namespace TORSEPAN.API.Services;

public interface INativeMessagePushTransport
{
    Task SendAsync(MessagePushSubscription subscription, Guid messageId, CancellationToken ct);
}
public sealed class NativeMessagePushConfiguration(IConfiguration configuration)
{
    public const string Prefix = "fcm:";
    public string AccountPath => configuration["Firebase:ServiceAccountPath"] ?? "";
    public object? Client => Configured ? new { appId = configuration["Firebase:AndroidAppId"], apiKey = configuration["Firebase:AndroidApiKey"], projectId = configuration["Firebase:ProjectId"], senderId = configuration["Firebase:SenderId"] } : null;
    private string AccountJson => configuration["Firebase:ServiceAccountJson"] ?? "";
    public async Task<string> ReadAccountAsync(CancellationToken ct) => !string.IsNullOrWhiteSpace(AccountJson) ? AccountJson : await File.ReadAllTextAsync(AccountPath, ct);
    public bool Configured => (!string.IsNullOrWhiteSpace(AccountJson) || File.Exists(AccountPath)) && new[] { "AndroidAppId", "AndroidApiKey", "ProjectId", "SenderId" }.All(k => !string.IsNullOrWhiteSpace(configuration["Firebase:" + k]));
    public string ProjectId => configuration["Firebase:ProjectId"] ?? "";
    public static bool ValidToken(string? value) => value is { Length: >= 20 and <= 1800 } && value.All(c => char.IsAsciiLetterOrDigit(c) || c is ':' or '_' or '-' or '.');
}
// Uses the existing per-message durable outbox. No Firebase secret enters an APK or browser response.
public sealed class NativeMessagePushTransport(IHttpClientFactory clients, NativeMessagePushConfiguration config) : INativeMessagePushTransport
{
    private readonly SemaphoreSlim gate = new(1,1);
    private string? accessToken;
    private DateTimeOffset expires;
    public async Task SendAsync(MessagePushSubscription subscription, Guid messageId, CancellationToken ct)
    {
        if (!config.Configured) throw new InvalidOperationException("Native message push is not configured.");
        var http = clients.CreateClient("native-message-push");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://fcm.googleapis.com/v1/projects/{Uri.EscapeDataString(config.ProjectId)}/messages:send");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",await AuthorizeAsync(http,ct));
        // Data-only messages allow the device to reject old-account/disabled enrollments before displaying anything.
        request.Content = JsonContent.Create(new { message = new { token = subscription.Endpoint[NativeMessagePushConfiguration.Prefix.Length..], data = new { kind="workshop-message", messageId=messageId.ToString(), userId=subscription.UserId.ToString(), enrollmentId=subscription.Auth }, android=new { priority="high", ttl="86400s" } } });
        using var response = await http.SendAsync(request,ct);
        if (response.IsSuccessStatusCode) return;
        var error = await response.Content.ReadAsStringAsync(ct);
        try
        {
            using var doc=JsonDocument.Parse(error);
            if (doc.RootElement.TryGetProperty("error",out var e) && e.TryGetProperty("details",out var details) && details.ValueKind==JsonValueKind.Array && details.EnumerateArray().Any(d=>d.TryGetProperty("errorCode",out var code)&&code.GetString()=="UNREGISTERED"))
                throw new MessagePushTransportException(HttpStatusCode.Gone);
        }
        catch(JsonException) { }
        throw new HttpRequestException("Native push delivery failed.",null,response.StatusCode);
    }
    private async Task<string> AuthorizeAsync(HttpClient http,CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            if(accessToken is not null && expires>DateTimeOffset.UtcNow.AddMinutes(2))return accessToken;
            using var account=JsonDocument.Parse(await config.ReadAccountAsync(ct));var root=account.RootElement;
            if(root.GetProperty("project_id").GetString()!=config.ProjectId)throw new InvalidOperationException("Firebase project mismatch.");
            static string Url(byte[] data)=>Convert.ToBase64String(data).TrimEnd('=').Replace('+','-').Replace('/','_');
            var now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var unsigned=Url(Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"typ\":\"JWT\"}"))+"."+Url(JsonSerializer.SerializeToUtf8Bytes(new { iss=root.GetProperty("client_email").GetString(),scope="https://www.googleapis.com/auth/firebase.messaging",aud="https://oauth2.googleapis.com/token",iat=now,exp=now+3600 }));
            using var rsa=RSA.Create();rsa.ImportFromPem(root.GetProperty("private_key").GetString()!);
            var assertion=unsigned+"."+Url(rsa.SignData(Encoding.UTF8.GetBytes(unsigned),HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1));
            using var response=await http.PostAsync("https://oauth2.googleapis.com/token",new FormUrlEncodedContent(new Dictionary<string,string>{{"grant_type","urn:ietf:params:oauth:grant-type:jwt-bearer"},{"assertion",assertion}}),ct);
            response.EnsureSuccessStatusCode();using var token=JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            accessToken=token.RootElement.GetProperty("access_token").GetString()!;expires=DateTimeOffset.UtcNow.AddSeconds(token.RootElement.GetProperty("expires_in").GetInt32());return accessToken;
        }
        finally{gate.Release();}
    }
}
