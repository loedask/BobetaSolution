using Bobeta.Application.DTOs.Auth;
using FluentValidation;

namespace Bobeta.Application.Validators;

public class RegisterPlayerRequestValidator : AbstractValidator<RegisterPlayerRequest>
{
    public RegisterPlayerRequestValidator()
    {
        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("Phone number is required.");
        RuleFor(x => x.PlayerName)
            .NotEmpty().WithMessage("Player name is required.")
            .MinimumLength(2).WithMessage("Player name must be at least 2 characters.")
            .MaximumLength(50).WithMessage("Player name must not exceed 50 characters.");
    }
}
