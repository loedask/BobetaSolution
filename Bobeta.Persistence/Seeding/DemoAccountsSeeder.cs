using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Bobeta.Persistence.Context;
using Bogus;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.Persistence.Seeding;

/// <summary>Inserts two demo players (and wallets) for local multiplayer testing. Names use <see cref="Faker"/> with a fixed seed (Laravel-style reproducible fake data).</summary>
public static class DemoAccountsSeeder
{
    /// <summary>Demo phone 1 — use Web country Congo (Brazzaville) +242; national digits 700000001.</summary>
    public const string DemoPhone1 = "+242700000001";

    /// <summary>Demo phone 2 — +242; national digits 700000002.</summary>
    public const string DemoPhone2 = "+242700000002";

    private const decimal DemoWalletBalance = 100_000m;

    /// <summary>Creates demo players if they do not already exist (idempotent).</summary>
    public static async Task SeedAsync(BobetaDbContext db, CancellationToken cancellationToken = default)
    {
        var faker = new Faker { Random = new Randomizer(42_001) };

        var seeds = new (string Phone, string Name)[]
        {
            (DemoPhone1, TruncatePlayerName($"{faker.Name.FirstName()} Demo")),
            (DemoPhone2, TruncatePlayerName($"{faker.Name.FirstName()} Test")),
        };

        foreach (var (phone, name) in seeds)
        {
            if (await db.Players.AnyAsync(p => p.PhoneNumber == phone, cancellationToken))
                continue;

            var playerId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            db.Players.Add(new Player
            {
                Id = playerId,
                PhoneNumber = phone,
                PlayerName = name,
                Language = "en",
                CreatedAt = now,
                IsVerified = true,
                Status = PlayerStatus.Active
            });

            db.Wallets.Add(new Wallet
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                Balance = DemoWalletBalance,
                LockedBalance = 0,
                UpdatedAt = now
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string TruncatePlayerName(string name) =>
        name.Length <= 50 ? name : name[..50];
}
