namespace Bobeta.Client.Presentation;

/// <summary>Localized user messages for API <see cref="GameMoveClientCodes"/>.</summary>
public static class GameMoveErrorMessages
{
    public static string? TryGetMessage(string? errorCode, Func<string, string> translate)
    {
        if (string.IsNullOrEmpty(errorCode))
            return null;

        return errorCode switch
        {
            GameMoveClientCodes.MustCapture => translate("invalid_move_kopo_must_capture"),
            GameMoveClientCodes.MustMaxCapture => translate("invalid_move_kopo_must_max_capture"),
            GameMoveClientCodes.MustContinueChain => translate("invalid_move_kopo_must_continue_chain"),
            GameMoveClientCodes.InvalidMove => translate("invalid_move_kopo_illegal"),
            _ => null
        };
    }
}
