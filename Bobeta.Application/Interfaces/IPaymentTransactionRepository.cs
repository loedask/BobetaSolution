using Bobeta.Domain.Entities;

namespace Bobeta.Application.Interfaces;

/// <summary>Persistence contract for MoMo payment transactions.</summary>
public interface IPaymentTransactionRepository
{
    /// <summary>Gets a payment transaction by id.</summary>
    Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets a payment transaction by MoMo reference id (X-Reference-Id).</summary>
    Task<PaymentTransaction?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken = default);

    /// <summary>Adds a new payment transaction.</summary>
    Task<PaymentTransaction> AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing payment transaction.</summary>
    Task UpdateAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>Gets ids of all payment transactions that are still Pending (for status polling worker).</summary>
    Task<IReadOnlyList<Guid>> GetPendingTransactionIdsAsync(CancellationToken cancellationToken = default);
}
