using Bobeta.Application.DTOs.Wallet;
using FluentValidation;

namespace Bobeta.Application.Validators;

public class WithdrawRequestValidator : AbstractValidator<WithdrawRequest>
{
    public WithdrawRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
    }
}
