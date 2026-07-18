namespace Bobeta.Application.Common;

/// <summary>Hard-coded demo player phones and seed wallet. Used for seeding and portal reset.</summary>
public static class DemoAccountConstants
{
    /// <summary>Demo phone 1. Web country Congo (Brazzaville) +242; national digits 700000001.</summary>
    public const string DemoPhone1 = "+242700000001";

    /// <summary>Demo phone 2. +242; national digits 700000002.</summary>
    public const string DemoPhone2 = "+242700000002";

    /// <summary>Wallet balance applied when seeding or resetting demo wallets.</summary>
    public const decimal DemoWalletBalance = 100_000m;

    /// <summary>All demo phone numbers. Reset and seed only ever target these.</summary>
    public static IReadOnlyList<string> PhoneNumbers { get; } = [DemoPhone1, DemoPhone2];
}
