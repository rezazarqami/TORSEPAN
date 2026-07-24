using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Application.Interfaces;

public interface IBowlRepository : IRepository<Bowl>
{
    Task<List<Bowl>> GetAllAsync(CancellationToken cancellationToken);

    Task<IEnumerable<Bowl>> GetAvailableBowlsAsync();

    Task<IEnumerable<Bowl>> GetWaitingForAssemblyAsync();

    Task<string?> GetLastProductionCodeAsync();
}