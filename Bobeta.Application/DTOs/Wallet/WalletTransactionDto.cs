using Bobeta.Domain.Enums;

namespace Bobeta.Application.DTOs.Wallet;

/// <summary>Single transaction entry for wallet history.</summary>
public record WalletTransactionDto(
    Guid Id,
    decimal Amount,
    TransactionType Type,
    TransactionStatus Status,
    string Reference,
    DateTime CreatedAt);
