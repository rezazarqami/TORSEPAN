using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly TORSEPANDbContext _context;

    public UnitOfWork(
        TORSEPANDbContext context,
        IUserRepository users,
        IHandpanRepository handpans,
        IBowlRepository bowls,
        IProductionEventRepository productionEvents)
    {
        _context = context;

        Users = users;
        Handpans = handpans;
        Bowls = bowls;
        ProductionEvents = productionEvents;
    }

    public IUserRepository Users { get; }

    public IHandpanRepository Handpans { get; }

    public IBowlRepository Bowls { get; }

    public IProductionEventRepository ProductionEvents { get; }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}