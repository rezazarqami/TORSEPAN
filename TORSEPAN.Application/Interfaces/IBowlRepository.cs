using System.Linq.Expressions;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Application.Interfaces;

public interface IBowlRepository : IRepository<Bowl>
{
    Task<List<Bowl>> GetAllAsync(CancellationToken cancellationToken);

    Task<IEnumerable<Bowl>> GetAvailableBowlsAsync();

    Task<IEnumerable<Bowl>> GetWaitingForAssemblyAsync();

    Task<string?> GetLastProductionCodeAsync();

    Task<bool> AnyAsync(
        Expression<Func<Bowl, bool>> predicate,
        CancellationToken cancellationToken = default);
}