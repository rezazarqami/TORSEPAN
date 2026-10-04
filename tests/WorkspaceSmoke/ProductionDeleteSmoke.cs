using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.JSInterop;
using TORSEPAN.Panel.Components.Pages;
using TORSEPAN.Panel.Models;
using TORSEPAN.Panel.Services;
using TORSEPAN.Panel.Services.Api;
using TORSEPAN.Panel.Services.Auth;

internal static class ProductionDeleteSmoke
{
    public static async Task RunAsync()
    {
        var api = new FixtureApi();
        var client = new ApiClient(new HttpClient(api) { BaseAddress = new Uri("https://fixture.invalid/api/") }, new TokenStorage(new FixtureJs()));
        var page = new Bowls();
        typeof(Bowls).GetProperty("BowlService", Flags)!.SetValue(page, new BowlService(client));
        typeof(Bowls).GetProperty("HandpanService", Flags)!.SetValue(page, new HandpanService(client));
        var bowl = new BowlDto { Id = Guid.NewGuid(), ProductionCode = "DELETE-TEST" };
        Request(page, bowl);
        Check(api.Deletes == 0 && Field(page, "_deleteBowl") == bowl, "delete click opens in-page confirmation without deleting or calling browser confirm");
        await Confirm(page);
        Check(Field(page, "_deleteBowl") == bowl && Field(page, "_deleteError") is string && Field(page, "_deleteNotice") is null,
            "failed deletion remains visible in the confirmation dialog and can be retried");
        api.Fail = false;
        await Confirm(page);
        Check(api.Deletes == 2 && api.LastDelete == "/api/bowls/" + bowl.Id && Field(page, "_deleteBowl") is null && Field(page, "_deleteNotice") is string,
            "confirmed retry deletes the chosen bowl and displays success");
        var handpan = new HandpanDto { Id = Guid.NewGuid(), SerialNumber = "INSTRUMENT-TEST" };
        Request(page, handpan);
        typeof(Bowls).GetMethod("CancelDelete", Flags)!.Invoke(page, null);
        Check(api.Deletes == 2 && Field(page, "_deleteHandpan") is null, "cancelling confirmation makes no delete request");
        Request(page, handpan); await Confirm(page);
        Check(api.LastDelete == "/api/production/" + handpan.Id && Field(page, "_deleteHandpan") is null,
            "instrument confirmation uses the existing authorized instrument deletion endpoint");
    }
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private static object? Field(object page, string name) => typeof(Bowls).GetField(name, Flags)!.GetValue(page);
    private static void Request(Bowls page, object record) => typeof(Bowls).GetMethod("RequestDelete", Flags, [record.GetType()])!.Invoke(page, [record]);
    private static Task Confirm(Bowls page) => (Task)typeof(Bowls).GetMethod("ConfirmDeleteAsync", Flags)!.Invoke(page, null)!;
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); Console.WriteLine("PASS " + message); }
    private sealed class FixtureJs : IJSRuntime
    {
        public ValueTask<T> InvokeAsync<T>(string id, object?[]? args) => InvokeAsync<T>(id, default, args);
        public ValueTask<T> InvokeAsync<T>(string id, CancellationToken ct, object?[]? args)
        { if (id == "confirm") throw new Exception("Browser confirm is unavailable"); return ValueTask.FromResult(default(T)!); }
    }
    private sealed class FixtureApi : HttpMessageHandler
    {
        public bool Fail = true;
        public int Deletes;
        public string? LastDelete;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.Method == HttpMethod.Delete)
            { Deletes++; LastDelete = request.RequestUri!.AbsolutePath; return Task.FromResult(new HttpResponseMessage(Fail ? HttpStatusCode.BadRequest : HttpStatusCode.NoContent)); }
            if (request.RequestUri!.AbsolutePath == "/api/bowls")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new PagedResult<BowlDto> { Page = 1, PageSize = 20 }) });
            if (request.RequestUri.AbsolutePath == "/api/handpans")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Array.Empty<HandpanDto>()) });
            throw new Exception("Unexpected request " + request.RequestUri);
        }
    }
}
