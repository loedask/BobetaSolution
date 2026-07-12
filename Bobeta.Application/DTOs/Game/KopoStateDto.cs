namespace Bobeta.Application.DTOs.Game;

public record KopoSquareDto(int Row, int Col);

public record KopoPieceDto(int Id, Guid OwnerId, int Row, int Col, bool IsKing);

public record KopoStateDto(
    int BoardSize,
    IReadOnlyList<KopoPieceDto> Pieces,
    bool MustContinueChain,
    int? ChainPieceId);

public record KopoMoveRequest(Guid SessionId, IReadOnlyList<KopoSquareDto> Path);
