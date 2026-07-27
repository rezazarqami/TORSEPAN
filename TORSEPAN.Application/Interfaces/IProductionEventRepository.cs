using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Application.Interfaces;

public interface IProductionEventRepository
{
    Task<ProductionEvent?> GetByIdAsync(Guid id);

    Task<List<ProductionEvent>> GetByHandpanIdAsync(Guid handpanId);

    Task AddAsync(ProductionEvent productionEvent);

    void Update(ProductionEvent productionEvent);

    void Remove(ProductionEvent productionEvent);
}