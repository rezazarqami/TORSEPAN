using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Interfaces;

public interface IHandpanRepository : IRepository<Handpan>
{
    Task<Handpan?> GetBySerialNumberAsync(string serialNumber);

    Task<Handpan?> GetForUpdateBySerialNumberAsync(string serialNumber);

    Task<IEnumerable<Handpan>> GetByStatusAsync(ProductionStatus status);

    Task<IEnumerable<Handpan>> GetReadyForPackagingAsync();

    Task<IEnumerable<Handpan>> GetWarehouseInventoryAsync();

    Task<IEnumerable<Handpan>> GetAllWithAssemblyAsync();
    Task<IEnumerable<Handpan>> GetSoldInventoryAsync();
}
