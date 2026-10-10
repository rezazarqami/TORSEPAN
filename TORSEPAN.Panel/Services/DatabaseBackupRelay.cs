using System.Net;
using System.Text.Json;

namespace TORSEPAN.Panel.Services;

public static class DatabaseBackupRelay
{
    public const long MaxRequestBytes = 49 * 1024 * 1024;
    public const long MaxFileBytes = 45 * 1024 * 1024;

    public static async Task<IResult> ForwardAsync(HttpRequest incoming, IConfiguration configuration,
        HttpClient client, CancellationToken cancellationToken)
    {
        var secret = configuration["TelegramRelay:Secret"];
        if (string.IsNullOrWhiteSpace(secret) || incoming.Headers["X-Relay-Secret"] != secret)
            return Results.Unauthorized();
        var token = configuration["TelegramRelay:BotToken"];
        var chat = configuration["TelegramRelay:ChatId"];
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chat))
            return Results.Problem("Telegram backup relay is not configured.", statusCode: 503);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromMinutes(5));
        try
        {
            var form = await incoming.ReadFormAsync(deadline.Token);
            var file = form.Files.GetFile("backup");
            if (file is null || file.Length == 0) return Results.BadRequest("Backup file is required.");
            if (file.Length > MaxFileBytes) return Results.StatusCode(413);
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(chat), "chat_id");
            content.Add(new StringContent("پشتیبان شبانه دیتابیس TORSEPAN"), "caption");
            await using var stream = file.OpenReadStream();
            content.Add(new StreamContent(stream), "document", Path.GetFileName(file.FileName));
            using var response = await client.PostAsync($"https://api.telegram.org/bot{token}/sendDocument", content, deadline.Token);
            if (!response.IsSuccessStatusCode)
                return Results.Problem($"Telegram rejected the backup (HTTP {(int)response.StatusCode}).",
                    statusCode: response.StatusCode == HttpStatusCode.TooManyRequests ? 429 : 502);
            using var receipt = JsonDocument.Parse(await response.Content.ReadAsStringAsync(deadline.Token));
            if (!receipt.RootElement.TryGetProperty("ok", out var ok) || ok.ValueKind != JsonValueKind.True)
                return Results.Problem("Telegram did not confirm backup delivery.", statusCode: 502);
            return Results.Ok(new { status = "sent" });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { return Results.Problem("Telegram backup delivery timed out.", statusCode: 504); }
        catch (HttpRequestException)
        { return Results.Problem("Backup relay cannot reach Telegram.", statusCode: 502); }
        catch (JsonException)
        { return Results.Problem("Telegram returned an invalid backup receipt.", statusCode: 502); }
        catch (BadHttpRequestException error)
        { return Results.Problem("Backup request could not be read.", statusCode: error.StatusCode); }
    }
}
