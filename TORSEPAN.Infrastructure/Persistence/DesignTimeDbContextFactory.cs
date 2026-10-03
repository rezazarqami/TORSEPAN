using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TORSEPAN.Infrastructure.Persistence;

// Migration generation does not run the API, background workers, or bootstrap administration.
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TORSEPANDbContext>
{
    public TORSEPANDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=127.0.0.1;Database=torsepan_design;Username=torsepan_design";
        return new TORSEPANDbContext(new DbContextOptionsBuilder<TORSEPANDbContext>().UseNpgsql(connection).Options);
    }
}
