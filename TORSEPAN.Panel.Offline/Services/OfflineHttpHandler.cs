using System.Text;
using System.Net;
using Microsoft.JSInterop;
namespace TORSEPAN.Panel.Services;
public sealed class OfflineHttpHandler(IJSRuntime js):HttpMessageHandler
{
 protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct){
 var uri=request.RequestUri!;
 if(uri.Scheme!="https"||uri.Host!="appassets.androidplatform.net"||!uri.AbsolutePath.StartsWith("/api/"))throw new HttpRequestException("نشانی محلی معتبر نیست.");
 var body=request.Content is null?null:await request.Content.ReadAsStringAsync(ct);
 var reply=await js.InvokeAsync<LocalReply>("OfflinePanel.request",ct,request.Method.Method,uri.PathAndQuery[5..],body);
 return new HttpResponseMessage((HttpStatusCode)reply.Status){Content=reply.Base64?new ByteArrayContent(Convert.FromBase64String(reply.Body)):new StringContent(reply.Body,Encoding.UTF8,reply.ContentType??"application/json"),RequestMessage=request};
 }
 public record LocalReply(int Status,string Body,string? ContentType=null,bool Base64=false);
}
