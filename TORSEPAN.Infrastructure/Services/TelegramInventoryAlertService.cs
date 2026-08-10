using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using TORSEPAN.Application.Interfaces;
namespace TORSEPAN.Infrastructure.Services;
public sealed class TelegramInventoryAlertService(HttpClient http, IConfiguration configuration) : IInventoryAlertService
{
    public async Task SendLowStockAsync(string itemName, string stockType, int quantity, int threshold, CancellationToken cancellationToken)
    {
        var token = configuration["Telegram:BotToken"]; var chatId = configuration["Telegram:ChatId"];
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chatId)) return;
        var text = $"⚠️ هشدار موجودی انبار مواد اولیه\n{itemName} - {stockType}\nموجودی فعلی: {quantity}\nحد هشدار: {threshold}";
        try { await http.PostAsJsonAsync($"https://api.telegram.org/bot{token}/sendMessage", new { chat_id = chatId, text }, cancellationToken); }
        catch { /* Inventory operations must not fail when Telegram is unavailable. */ }
    }
}
