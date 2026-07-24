namespace TORSEPAN.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }

    IBowlRepository Bowls { get; }

    IHandpanRepository Handpans { get; }

    IHandpanAssemblyRepository HandpanAssemblies { get; }

    IProductionEventRepository ProductionEvents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync();

    Task CommitTransactionAsync();

    Task RollbackTransactionAsync();
}