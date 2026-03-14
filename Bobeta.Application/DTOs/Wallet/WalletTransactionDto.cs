using Bobeta.Domain.Enums;

namespace Bobeta.Application.DTOs.Wallet;

public record WalletTransactionDto(
    Guid Id,
    decimal Amount,
    TransactionType Type,
    TransactionStatus Status,
    string Reference,
    DateTime CreatedAt);
