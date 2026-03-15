using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bobeta.Persistence.Context;

/// <summary>Design-time factory for creating a DbContext when running EF Core tools (e.g. migrations).</summary>
public class BobetaDbContextFactory : IDesignTimeDbContextFactory<BobetaDbContext>
{
    /// <inheritdoc />
    public BobetaDbContext CreateDbContext(string[] args)
    {
        const string connectionString = "Host=localhost;Database=Bobeta;Username=postgres;Password=daskana";

        var options = new DbContextOptionsBuilder<BobetaDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new BobetaDbContext(options);
    }
}
