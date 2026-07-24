using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Application.Interfaces;

public interface IProductionEventRepository : IRepository<ProductionEvent>
{
    Task<IEnumerable<ProductionEvent>> GetByHandpanIdAsync(Guid handpanId);

    Task<IEnumerable<ProductionEvent>> GetRecentEventsAsync(int count = 100);
}