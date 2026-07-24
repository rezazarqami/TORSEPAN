using Microsoft.EntityFrameworkCore.Storage;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly TORSEPANDbContext _context;
    private IDbContextTransaction? _transaction;

    public IUserRepository Users { get; }

    public IBowlRepository Bowls { get; }

    public IHandpanRepository Handpans { get; }

    public IHandpanAssemblyRepository HandpanAssemblies { get; }

    public IProductionEventRepository ProductionEvents { get; }

    public UnitOfWork(
        TORSEPANDbContext context,
        IUserRepository users,
        IBowlRepository bowls,
        IHandpanRepository handpans,
        IHandpanAssemblyRepository handpanAssemblies,
        IProductionEventRepository productionEvents)
    {
        _context = context;

        Users = users;
        Bowls = bowls;
        Handpans = handpans;
        HandpanAssemblies = handpanAssemblies;
        ProductionEvents = productionEvents;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync()
    {
        if (_transaction != null)
            return;

        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction == null)
            return;

        await _context.SaveChangesAsync();
        await _transaction.CommitAsync();

        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction == null)
            return;

        await _transaction.RollbackAsync();

        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}