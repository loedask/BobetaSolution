using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.Persistence.Context;

/// <summary>Entity Framework Core database context for the Bobeta platform. Uses PostgreSQL.</summary>
public class BobetaDbContext : DbContext
{
    /// <summary>Creates a new context with the given options (connection string configured in DI).</summary>
    public BobetaDbContext(DbContextOptions<BobetaDbContext> options) : base(options) { }

    /// <summary>Registered players.</summary>
    public DbSet<Player> Players => Set<Player>();
    /// <summary>Player wallets (one per player).</summary>
    public DbSet<Wallet> Wallets => Set<Wallet>();

    /// <summary>Wallet transaction history.</summary>
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();

    /// <summary>Game sessions (Makopa matches).</summary>
    public DbSet<GameSession> GameSessions => Set<GameSession>();

    /// <summary>Individual card plays.</summary>
    public DbSet<GameMove> GameMoves => Set<GameMove>();

    /// <summary>Game results (when a game finishes).</summary>
    public DbSet<GameResult> GameResults => Set<GameResult>();

    /// <summary>OTP codes for phone verification.</summary>
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    /// <summary>MoMo payment transactions (deposit/withdrawal).</summary>
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BobetaDbContext).Assembly);
    }
}
