using Bobeta.Domain.Entities;

namespace Bobeta.Application.Interfaces;

/// <summary>Persistence contract for SMS messages (store sent SMS, update status on DLR).</summary>
public interface ISmsMessageRepository
{
    /// <summary>Adds a new SMS message record.</summary>
    Task<SmsMessage> AddAsync(SmsMessage sms, CancellationToken cancellationToken = default);

    /// <summary>Gets an SMS message by provider message ID, or null if not found.</summary>
    Task<SmsMessage?> GetByProviderMessageIdAsync(string providerMessageId, CancellationToken cancellationToken = default);

    /// <summary>Gets an SMS message by its primary key, or null if not found.</summary>
    Task<SmsMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing SMS message (e.g. status from DLR).</summary>
    Task UpdateAsync(SmsMessage sms, CancellationToken cancellationToken = default);
}
