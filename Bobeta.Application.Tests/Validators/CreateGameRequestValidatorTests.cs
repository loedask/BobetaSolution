using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Validators;
using Bobeta.Domain.Enums;
using FluentValidation.TestHelper;
using Xunit;

namespace Bobeta.Application.Tests.Validators;

public sealed class CreateGameRequestValidatorTests
{
  private readonly CreateGameRequestValidator _validator = new();

  [Theory]
  [InlineData(200)]
  [InlineData(300)]
  [InlineData(500)]
  public void Validate_AcceptsBetWithinRange(decimal betAmount)
  {
    var request = new CreateGameRequest(betAmount, GameVariant.Makopa);

    var result = _validator.TestValidate(request);

    result.ShouldNotHaveAnyValidationErrors();
  }

  [Theory]
  [InlineData(199)]
  [InlineData(501)]
  public void Validate_RejectsBetOutsideRange(decimal betAmount)
  {
    var request = new CreateGameRequest(betAmount, GameVariant.Makopa);

    var result = _validator.TestValidate(request);

    result.ShouldHaveValidationErrorFor(x => x.BetAmount);
  }

  [Theory]
  [InlineData(GameVariant.Makopa)]
  [InlineData(GameVariant.Kopo)]
  public void Validate_AcceptsAllVariants(GameVariant variant)
  {
    var request = new CreateGameRequest(200, variant);

    var result = _validator.TestValidate(request);

    result.ShouldNotHaveAnyValidationErrors();
  }
}
