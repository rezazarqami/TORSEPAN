using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.Infrastructure.Services;

public sealed class OrderCustomerLookup(TORSEPANDbContext db) : IOrderCustomerLookup
{
    public async Task<IReadOnlyDictionary<Guid,string?>> GetHandpanCustomerNamesAsync(IReadOnlyCollection<Guid> handpanIds, CancellationToken ct)
    {
        var rows = await db.Handpans.AsNoTracking().Where(handpan => handpanIds.Contains(handpan.Id))
            .Select(handpan => new { handpan.Id, Name = db.CustomerOrders.Where(order => !order.IsDraft &&
                (order.TopBowlId == handpan.Assembly.TopBowlId || order.HandpanId == handpan.Id ||
                 db.CustomerOrderLines.Any(line => line.OrderId == order.Id &&
                    db.OrderInstruments.Any(instrument => instrument.LineId == line.Id &&
                        (instrument.TopBowlId == handpan.Assembly.TopBowlId || instrument.HandpanId == handpan.Id)))))
                .OrderByDescending(order => order.CreatedAtUtc).ThenBy(order => order.Id)
                .Select(order => order.CustomerName).FirstOrDefault() }).ToListAsync(ct);
        return rows.ToDictionary(x => x.Id, x => x.Name);
    }

    public Task<string?> GetCustomerNameAsync(Guid topBowlId, Guid? handpanId, CancellationToken ct) =>
        db.CustomerOrders.AsNoTracking().Where(order => !order.IsDraft &&
            (order.TopBowlId == topBowlId || (handpanId.HasValue && order.HandpanId == handpanId) ||
             db.CustomerOrderLines.Any(line => line.OrderId == order.Id &&
                db.OrderInstruments.Any(instrument => instrument.LineId == line.Id &&
                    (instrument.TopBowlId == topBowlId || (handpanId.HasValue && instrument.HandpanId == handpanId))))))
            .OrderByDescending(order => order.CreatedAtUtc).ThenBy(order => order.Id)
            .Select(order => order.CustomerName).FirstOrDefaultAsync(ct);
}
