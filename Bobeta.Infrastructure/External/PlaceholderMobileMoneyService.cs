using Bobeta.Application.Interfaces;

namespace Bobeta.Infrastructure.External;

/// <summary>Placeholder implementation of Mobile Money; returns success and a reference without calling a real provider. Replace with actual MoMo integration in production.</summary>
public class PlaceholderMobileMoneyService : IMobileMoneyService
{
    /// <inheritdoc />
    public Task<MobileMoneyResult> RequestDepositAsync(string phoneNumber, decimal amount, CancellationToken cancellationToken = default)
    {
        var reference = Guid.NewGuid().ToString("N")[..12];
        return Task.FromResult(new MobileMoneyResult(true, reference, "Placeholder: confirm via MoMo provider."));
    }

    /// <inheritdoc />
    public Task<MobileMoneyResult> ConfirmDepositAsync(string reference, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new MobileMoneyResult(true, reference, "Placeholder: deposit confirmed."));
    }

    /// <inheritdoc />
    public Task<MobileMoneyResult> SendWithdrawalAsync(string phoneNumber, decimal amount, CancellationToken cancellationToken = default)
    {
        var reference = Guid.NewGuid().ToString("N")[..12];
        return Task.FromResult(new MobileMoneyResult(true, reference, "Placeholder: withdrawal initiated."));
    }
}
