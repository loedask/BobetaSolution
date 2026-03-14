using Bobeta.Application.DTOs.Game;
using FluentValidation;

namespace Bobeta.Application.Validators;

public class ProposeBetRequestValidator : AbstractValidator<ProposeBetRequest>
{
    public ProposeBetRequestValidator()
    {
        RuleFor(x => x.GameId).NotEmpty();
        RuleFor(x => x.Amount)
            .InclusiveBetween(CreateGameRequestValidator.MinBet, CreateGameRequestValidator.MaxBet)
            .WithMessage($"Bet amount must be between {CreateGameRequestValidator.MinBet} and {CreateGameRequestValidator.MaxBet}.");
    }
}
