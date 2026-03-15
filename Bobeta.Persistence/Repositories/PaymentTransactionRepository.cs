using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Bobeta.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.Persistence.Repositories;

/// <summary>Repository for MoMo payment transactions.</summary>
public class PaymentTransactionRepository(BobetaDbContext context) : IPaymentTransactionRepository
{
    private readonly BobetaDbContext _context = context;

    /// <inheritdoc />
    public async Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentTransactions
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PaymentTransaction?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentTransactions
            .FirstOrDefaultAsync(p => p.ExternalReference == externalReference, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PaymentTransaction> AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default)
    {
        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default)
    {
        _context.PaymentTransactions.Update(transaction);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetPendingTransactionIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PaymentTransactions
            .Where(p => p.Status == PaymentTransactionStatus.Pending)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
    }
}
