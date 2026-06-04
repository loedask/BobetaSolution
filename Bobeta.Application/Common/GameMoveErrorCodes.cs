namespace Bobeta.Application.Common;

/// <summary>Machine-readable reasons when a play-card or take move is rejected.</summary>
public static class GameMoveErrorCodes
{
    public const string InvalidState = "invalid_state";
    public const string NotYourTurn = "not_your_turn";
    public const string CardNotInHand = "card_not_in_hand";
    public const string MustFollowSuit = "must_follow_suit";
    public const string MustTake = "must_take";
    public const string InvalidTrick = "invalid_trick";
    public const string InvalidMove = "invalid_move";
    public const string MustCapture = "must_capture";
    public const string MustMaxCapture = "must_max_capture";
    public const string MustContinueChain = "must_continue_chain";
}
