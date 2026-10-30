using System.Net;
using Microsoft.JSInterop;
using TORSEPAN.Panel.Services;
using TORSEPAN.Panel.Services.Auth;

static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
var clock = new Clock();
var cache = new LookupResponseCache(clock);
var calls = 0;
Task<string> Load() { calls++; return Task.FromResult("[1,2]"); }
var first = await cache.GetAsync<List<int>>("a", "scales", Load);
first!.Clear();
Check((await cache.GetAsync<List<int>>("a", "scales", Load))!.Count == 2 && calls == 1, "clone/cache hit");
clock.Now = clock.Now.AddSeconds(61);
await cache.GetAsync<List<int>>("a", "scales", Load);
Check(calls == 2, "expiry");
await cache.GetAsync<List<int>>("b", "scales", Load);
Check(calls == 3, "identity isolation");
cache.Clear();
var pending = new TaskCompletionSource<string>();
var a = cache.GetAsync<List<int>>("b", "scales", () => { calls++; return pending.Task; });
var b = cache.GetAsync<List<int>>("b", "scales", Load);
pending.SetResult("[3]");
await Task.WhenAll(a,b);
Check(calls == 4, "concurrent request deduplication");
cache.Clear();
try { await cache.GetAsync<List<int>>("b", "scales", () => Task.FromException<string>(new Exception())); } catch { }
await cache.GetAsync<List<int>>("b", "scales", Load);
Check(calls == 5, "errors are retried");
cache.Clear();
var old = new TaskCompletionSource<string>();
var stale = cache.GetAsync<List<int>>("b", "scales", () => old.Task);
cache.Clear();
await cache.GetAsync<List<int>>("b", "scales", Load);
old.SetResult("[999]"); await stale;
Check((await cache.GetAsync<List<int>>("b", "scales", Load))![0] == 1, "invalidated in-flight read cannot repopulate cache");

var js = new Storage(); var handler = new Handler();
var api = new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://test/api/") }, new TokenStorage(js));
await api.GetAsync<List<int>>("scales"); await api.GetAsync<List<int>>("scales");
Check(handler.Reads == 1, "catalog HTTP cache");
await api.GetAsync<List<int>>("production/warehouse"); await api.GetAsync<List<int>>("production/warehouse");
Check(handler.Reads == 3, "inventory stays live");
await api.PostAsync<object, object?>("scales", new {});
await api.GetAsync<List<int>>("scales"); Check(handler.Reads == 4, "mutation invalidation");
await api.GetAsync<List<int>>("scales", forceRefresh: true); Check(handler.Reads == 5, "explicit refresh");
js.Token = "other"; await api.GetAsync<List<int>>("scales"); Check(handler.Reads == 6, "account change invalidation");
handler.FailNext = true;
try { await api.DeleteAsync("scales/1"); } catch (HttpRequestException) { }
await api.GetAsync<List<int>>("scales"); Check(handler.Reads == 7, "failed mutation invalidation");
foreach (var write in new Func<Task>[] {
    async () => { await api.PutAsync<object, object?>("scales/1", new {}); },
    async () => { await api.PatchAsync<object, object?>("scales/1", new {}); },
    async () => { await api.OrderActionAsync<object, object?>("orders/1", new {}); },
    async () => { await api.PutAccountAsync<object, object?>("users/1", new {}); },
    () => api.DeleteAccountAsync("users/1"),
    async () => { await api.PostFileAsync<object?>("photos", [1], [1], "test"); }
})
{
    var before = handler.Reads;
    await write(); await api.GetAsync<List<int>>("scales");
    Check(handler.Reads == before + 1, "every mutation API invalidates lookups");
}
Console.WriteLine("PASS: lookup expiry, isolation, cloning, deduplication, retry, in-flight invalidation, live inventory and API writes.");
sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
sealed class Storage : IJSRuntime {
    public string Token = "first";
    public ValueTask<T> InvokeAsync<T>(string name, object?[]? args) => ValueTask.FromResult((T)(object)Token);
    public ValueTask<T> InvokeAsync<T>(string name, CancellationToken ct, object?[]? args) => InvokeAsync<T>(name,args);
}
sealed class Handler : HttpMessageHandler {
    public int Reads; public bool FailNext;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) {
        if (request.Method == HttpMethod.Get) { Reads++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[1,2]") }); }
        var status = FailNext ? HttpStatusCode.Conflict : HttpStatusCode.NoContent; FailNext = false;
        return Task.FromResult(new HttpResponseMessage(status));
    }
}
