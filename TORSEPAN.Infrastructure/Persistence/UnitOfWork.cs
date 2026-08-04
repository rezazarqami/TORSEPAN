using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly TORSEPANDbContext _context;

    public UnitOfWork(
        TORSEPANDbContext context,
        IUserRepository users,
        IRoleRepository roles,
        IUserRoleRepository userRoles,
        IRefreshTokenRepository refreshTokens,
        IHandpanRepository handpans,
        IHandpanAssemblyRepository handpanAssemblies,
        IBowlRepository bowls,
        IMaterialRepository materials,
        IProductionEventRepository productionEvents)
    {
        _context = context;

        Users = users;
        Roles = roles;
        UserRoles = userRoles;
        RefreshTokens = refreshTokens;
        Handpans = handpans;
        HandpanAssemblies = handpanAssemblies;
        Bowls = bowls;
        Materials = materials;
        ProductionEvents = productionEvents;
    }

    public IUserRepository Users { get; }

    public IRoleRepository Roles { get; }

    public IUserRoleRepository UserRoles { get; }

    public IRefreshTokenRepository RefreshTokens { get; }

    public IHandpanRepository Handpans { get; }

    public IHandpanAssemblyRepository HandpanAssemblies { get; }

    public IBowlRepository Bowls { get; }

    public IMaterialRepository Materials { get; }

    public IProductionEventRepository ProductionEvents { get; }

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}