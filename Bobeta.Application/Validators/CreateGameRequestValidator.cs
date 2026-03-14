using Bobeta.Application.DTOs.Game;
using FluentValidation;

namespace Bobeta.Application.Validators;

public class CreateGameRequestValidator : AbstractValidator<CreateGameRequest>
{
    public const decimal MinBet = 200;
    public const decimal MaxBet = 500;

    public CreateGameRequestValidator()
    {
        RuleFor(x => x.BetAmount)
            .InclusiveBetween(MinBet, MaxBet)
            .WithMessage($"Bet amount must be between {MinBet} and {MaxBet}.");
    }
}
