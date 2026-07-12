using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.Persistence.Context;

/// <summary>Entity Framework Core database context for the Bobeta platform. Uses PostgreSQL.</summary>
/// <remarks>Creates a new context with the given options (connection string configured in DI).</remarks>
public class BobetaDbContext(DbContextOptions<BobetaDbContext> options) : DbContext(options)
{

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

    /// <summary>SMS messages sent via gateway (for DLR tracking).</summary>
    public DbSet<SmsMessage> SmsMessages => Set<SmsMessage>();

    /// <summary>Bobeta.Portal staff accounts.</summary>
    public DbSet<PortalUser> PortalUsers => Set<PortalUser>();

    /// <summary>Contracted country license holders.</summary>
    public DbSet<LicensePartner> LicensePartners => Set<LicensePartner>();

    /// <summary>Country assignments per license partner.</summary>
    public DbSet<LicensePartnerCountryAssignment> LicensePartnerCountryAssignments => Set<LicensePartnerCountryAssignment>();

    /// <summary>Effective-dated revenue share rates per assignment.</summary>
    public DbSet<LicensePartnerRevenueShareRate> LicensePartnerRevenueShareRates => Set<LicensePartnerRevenueShareRate>();

    /// <summary>Revenue split ledger entries.</summary>
    public DbSet<RevenueAllocation> RevenueAllocations => Set<RevenueAllocation>();

    /// <summary>Influencer marketing partners.</summary>
    public DbSet<Influencer> Influencers => Set<Influencer>();

    /// <summary>Player redemptions of influencer invite codes.</summary>
    public DbSet<InfluencerCodeRedemption> InfluencerCodeRedemptions => Set<InfluencerCodeRedemption>();

    /// <summary>Influencer game commission ledger.</summary>
    public DbSet<InfluencerCommissionAllocation> InfluencerCommissionAllocations => Set<InfluencerCommissionAllocation>();

    /// <summary>Platform key/value settings.</summary>
    public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BobetaDbContext).Assembly);
    }
}
