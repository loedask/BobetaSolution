using Bobeta.Application.DTOs.Wallet;
using FluentValidation;

namespace Bobeta.Application.Validators;

public class DepositRequestValidator : AbstractValidator<DepositRequest>
{
    public DepositRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
    }
}
