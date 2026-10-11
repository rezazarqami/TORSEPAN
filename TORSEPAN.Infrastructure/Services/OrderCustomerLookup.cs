using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.Infrastructure.Services;

public sealed class OrderCustomerLookup(TORSEPANDbContext db) : IOrderCustomerLookup
{
    public Task<string?> GetCustomerNameAsync(Guid topBowlId, Guid? handpanId, CancellationToken ct) =>
        db.CustomerOrders.AsNoTracking().Where(order => !order.IsDraft &&
            (order.TopBowlId == topBowlId || (handpanId.HasValue && order.HandpanId == handpanId) ||
             db.CustomerOrderLines.Any(line => line.OrderId == order.Id &&
                db.OrderInstruments.Any(instrument => instrument.LineId == line.Id &&
                    (instrument.TopBowlId == topBowlId || (handpanId.HasValue && instrument.HandpanId == handpanId))))))
            .OrderByDescending(order => order.CreatedAtUtc).ThenBy(order => order.Id)
            .Select(order => order.CustomerName).FirstOrDefaultAsync(ct);
}
