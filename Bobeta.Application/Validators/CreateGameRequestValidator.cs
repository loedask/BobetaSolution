using Bobeta.Application.DTOs.Game;
using FluentValidation;

namespace Bobeta.Application.Validators;

/// <summary>Validates create-game requests: bet amount must be within platform limits (200–500).</summary>
public class CreateGameRequestValidator : AbstractValidator<CreateGameRequest>
{
    /// <summary>Minimum allowed bet amount.</summary>
    public const decimal MinBet = 200;

    /// <summary>Maximum allowed bet amount.</summary>
    public const decimal MaxBet = 500;

    /// <inheritdoc />
    public CreateGameRequestValidator()
    {
        RuleFor(x => x.BetAmount)
            .InclusiveBetween(MinBet, MaxBet)
            .WithMessage($"Bet amount must be between {MinBet} and {MaxBet}.");
    }
}
