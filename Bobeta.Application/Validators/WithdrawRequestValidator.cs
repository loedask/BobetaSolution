using Bobeta.Application.DTOs.Wallet;
using FluentValidation;

namespace Bobeta.Application.Validators;

/// <summary>Validates withdrawal requests: amount must be greater than zero.</summary>
public class WithdrawRequestValidator : AbstractValidator<WithdrawRequest>
{
    /// <inheritdoc />
    public WithdrawRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
    }
}
