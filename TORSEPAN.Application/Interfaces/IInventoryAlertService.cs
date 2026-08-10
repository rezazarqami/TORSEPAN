namespace TORSEPAN.Application.Interfaces;
public interface IInventoryAlertService { Task SendLowStockAsync(string itemName, string stockType, int quantity, int threshold, CancellationToken cancellationToken); }
