using Bobeta.Application.DTOs.Game;
using FluentValidation;

namespace Bobeta.Application.Validators;

/// <summary>Validates bet proposal: game id required; amount within platform limits (200–500).</summary>
public class ProposeBetRequestValidator : AbstractValidator<ProposeBetRequest>
{
    /// <inheritdoc />
    public ProposeBetRequestValidator()
    {
        RuleFor(x => x.GameId).NotEmpty();
        RuleFor(x => x.Amount)
            .InclusiveBetween(CreateGameRequestValidator.MinBet, CreateGameRequestValidator.MaxBet)
            .WithMessage($"Bet amount must be between {CreateGameRequestValidator.MinBet} and {CreateGameRequestValidator.MaxBet}.");
    }
}
