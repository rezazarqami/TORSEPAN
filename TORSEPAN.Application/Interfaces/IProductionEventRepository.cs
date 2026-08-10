using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Interfaces;

public interface IProductionEventRepository
{
    Task<ProductionEvent?> GetByIdAsync(Guid id);
    Task<List<ProductionEvent>> GetByHandpanIdAsync(Guid handpanId);
    Task<List<ProductionEvent>> GetReportAsync(
        DateTime? from, DateTime? to, Guid? userId,
        ProductionAction? action, EventResult? result);
    Task AddAsync(ProductionEvent productionEvent);
    void Update(ProductionEvent productionEvent);
    void Remove(ProductionEvent productionEvent);
}
