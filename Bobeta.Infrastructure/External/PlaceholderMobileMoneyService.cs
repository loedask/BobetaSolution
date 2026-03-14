using Bobeta.Application.Interfaces;

namespace Bobeta.Infrastructure.External;

public class PlaceholderMobileMoneyService : IMobileMoneyService
{
    public Task<MobileMoneyResult> RequestDepositAsync(string phoneNumber, decimal amount, CancellationToken cancellationToken = default)
    {
        var reference = Guid.NewGuid().ToString("N")[..12];
        return Task.FromResult(new MobileMoneyResult(true, reference, "Placeholder: confirm via MoMo provider."));
    }

    public Task<MobileMoneyResult> ConfirmDepositAsync(string reference, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new MobileMoneyResult(true, reference, "Placeholder: deposit confirmed."));
    }

    public Task<MobileMoneyResult> SendWithdrawalAsync(string phoneNumber, decimal amount, CancellationToken cancellationToken = default)
    {
        var reference = Guid.NewGuid().ToString("N")[..12];
        return Task.FromResult(new MobileMoneyResult(true, reference, "Placeholder: withdrawal initiated."));
    }
}
