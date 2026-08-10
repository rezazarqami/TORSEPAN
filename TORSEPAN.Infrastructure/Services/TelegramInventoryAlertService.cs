using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TORSEPAN.Application.Interfaces;
namespace TORSEPAN.Infrastructure.Services;
public sealed class TelegramInventoryAlertService(HttpClient http, IConfiguration configuration, ILogger<TelegramInventoryAlertService> logger) : IInventoryAlertService
{
    public async Task SendLowStockAsync(string itemName, string stockType, int quantity, int threshold, CancellationToken cancellationToken)
    {
        var token = configuration["Telegram:BotToken"]; var chatId = configuration["Telegram:ChatId"];
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chatId)) return;
        var text = $"âš ï¸ Ù‡Ø´Ø¯Ø§Ø± Ù…ÙˆØ¬ÙˆØ¯ÛŒ Ø§Ù†Ø¨Ø§Ø± Ù…ÙˆØ§Ø¯ Ø§ÙˆÙ„ÛŒÙ‡\n{itemName} - {stockType}\nÙ…ÙˆØ¬ÙˆØ¯ÛŒ ÙØ¹Ù„ÛŒ: {quantity}\nØ­Ø¯ Ù‡Ø´Ø¯Ø§Ø±: {threshold}";
        try
        {
            var response = await http.PostAsJsonAsync($"https://api.telegram.org/bot{token}/sendMessage", new { chat_id = chatId, text }, cancellationToken);
            response.EnsureSuccessStatusCode();
            logger.LogInformation("Low stock Telegram alert sent for {ItemName}.", itemName);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Sending low stock Telegram alert failed for {ItemName}.", itemName);
        }
    }
}

