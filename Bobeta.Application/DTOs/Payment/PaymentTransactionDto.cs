using Bobeta.Domain.Enums;

namespace Bobeta.Application.DTOs.Payment;

/// <summary>DTO for a payment transaction (MoMo deposit or withdrawal).</summary>
public record PaymentTransactionDto(
    Guid Id,
    Guid PlayerId,
    decimal Amount,
    string Currency,
    string ExternalReference,
    string? MoMoTransactionId,
    PaymentTransactionType Type,
    PaymentTransactionStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
