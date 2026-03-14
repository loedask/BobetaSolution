using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.Persistence.Context;

public class BobetaDbContext : DbContext
{
    public BobetaDbContext(DbContextOptions<BobetaDbContext> options) : base(options) { }

    public DbSet<Player> Players => Set<Player>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<GameMove> GameMoves => Set<GameMove>();
    public DbSet<GameResult> GameResults => Set<GameResult>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BobetaDbContext).Assembly);
    }
}
