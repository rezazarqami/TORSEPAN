using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TORSEPAN.Application.Interfaces;
namespace TORSEPAN.Infrastructure.Services;
public sealed class TelegramInventoryAlertService(HttpClient http, IConfiguration configuration, ILogger<TelegramInventoryAlertService> logger) : IInventoryAlertService
{
    public Task SendLowStockAsync(string itemName, string stockType, int quantity, int threshold, CancellationToken cancellationToken)
    {
        _ = SendInBackgroundAsync(itemName, stockType, quantity, threshold);
        return Task.CompletedTask;
    }

    private async Task SendInBackgroundAsync(string itemName, string stockType, int quantity, int threshold)
    {
        var token = configuration["Telegram:BotToken"]; var chatId = configuration["Telegram:ChatId"];
        var text = $"⚠️ هشدار موجودی انبار مواد اولیه\n{itemName} - {stockType}\nموجودی فعلی: {quantity}\nحد هشدار: {threshold}";
        try
        {
            var relayUrl = configuration["Telegram:RelayUrl"];
            HttpResponseMessage response;
            if (!string.IsNullOrWhiteSpace(relayUrl))
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, relayUrl)
                {
                    Content = Utf8Json(new { itemName, stockType, quantity, threshold })
                };
                request.Headers.Add("X-Relay-Secret", configuration["Telegram:RelaySecret"]);
                response = await http.SendAsync(request, CancellationToken.None);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chatId)) return;
                response = await http.PostAsync($"https://api.telegram.org/bot{token}/sendMessage", Utf8Json(new { chat_id = chatId, text }), CancellationToken.None);
            }
            response.EnsureSuccessStatusCode();
            logger.LogInformation("Low stock Telegram alert sent for {ItemName}.", itemName);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Sending low stock Telegram alert failed for {ItemName}.", itemName);
        }
    }

    private static StringContent Utf8Json<T>(T value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
}

