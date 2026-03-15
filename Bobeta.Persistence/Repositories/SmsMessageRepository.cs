using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.Persistence.Repositories;

/// <summary>Repository implementation for SmsMessage (add, get by provider ID, update).</summary>
public class SmsMessageRepository(BobetaDbContext db) : ISmsMessageRepository
{
    private readonly BobetaDbContext _db = db;

    /// <inheritdoc />
    public async Task<SmsMessage> AddAsync(SmsMessage sms, CancellationToken cancellationToken = default)
    {
        _db.SmsMessages.Add(sms);
        await _db.SaveChangesAsync(cancellationToken);
        return sms;
    }

    /// <inheritdoc />
    public async Task<SmsMessage?> GetByProviderMessageIdAsync(string providerMessageId, CancellationToken cancellationToken = default) =>
        await _db.SmsMessages
            .FirstOrDefaultAsync(s => s.ProviderMessageId == providerMessageId, cancellationToken);

    /// <inheritdoc />
    public async Task UpdateAsync(SmsMessage sms, CancellationToken cancellationToken = default)
    {
        _db.SmsMessages.Update(sms);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
