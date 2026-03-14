using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bobeta.Persistence.Context;

public class BobetaDbContextFactory : IDesignTimeDbContextFactory<BobetaDbContext>
{
    public BobetaDbContext CreateDbContext(string[] args)
    {
        const string connectionString = "Host=localhost;Database=Bobeta;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<BobetaDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new BobetaDbContext(options);
    }
}
