namespace Bobeta.Client.Presentation;

/// <summary>Matches <see cref="Bobeta.Application.Common.GameMoveErrorCodes"/> from the API.</summary>
public static class GameMoveClientCodes
{
    public const string NotYourTurn = "not_your_turn";
    public const string MustFollowSuit = "must_follow_suit";
    public const string MustTake = "must_take";
    public const string CardNotInHand = "card_not_in_hand";
    public const string InvalidTrick = "invalid_trick";
    public const string InvalidState = "invalid_state";
}
