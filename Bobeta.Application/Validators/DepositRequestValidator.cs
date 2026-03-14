using Bobeta.Application.DTOs.Wallet;
using FluentValidation;

namespace Bobeta.Application.Validators;

/// <summary>Validates deposit requests: amount must be greater than zero.</summary>
public class DepositRequestValidator : AbstractValidator<DepositRequest>
{
    /// <inheritdoc />
    public DepositRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
    }
}
