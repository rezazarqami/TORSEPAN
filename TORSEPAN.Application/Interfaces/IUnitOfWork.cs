using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Interfaces;

public interface IUnitOfWork
{
    IUserRepository Users { get; }

    IHandpanRepository Handpans { get; }

    IBowlRepository Bowls { get; }

    IProductionEventRepository ProductionEvents { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}