using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Application.Interfaces;

public interface IHandpanAssemblyRepository : IRepository<HandpanAssembly>
{
    Task<HandpanAssembly?> GetByHandpanIdAsync(Guid handpanId);

    Task<IEnumerable<HandpanAssembly>> GetPendingAssembliesAsync();
}