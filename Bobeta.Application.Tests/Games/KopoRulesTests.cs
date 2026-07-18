using Bobeta.Application.Common;
using Bobeta.Application.Games.Kopo;
using Xunit;

namespace Bobeta.Application.Tests.Games;

/// <summary>Regression tests for Kopo board setup, movement, capture, and outcome rules.</summary>
public class KopoRulesTests
{
    private readonly Guid _creator = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _opponent = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void CreateInitial_Places40PiecesOnDarkSquares()
    {
        var state = KopoRules.CreateInitial(_creator, _opponent, _creator);
        Assert.Equal(40, state.Pieces.Count);
        Assert.Equal(20, state.Pieces.Count(p => p.OwnerId == _creator));
        Assert.Equal(20, state.Pieces.Count(p => p.OwnerId == _opponent));
        Assert.All(state.Pieces, p => Assert.True(KopoBoard.IsPlayable(p.Row, p.Col)));
    }

    [Fact]
    public void KopoBoard_PlayableSquares_AreDarkCellsOn10x10Grid()
    {
        Assert.Equal(10, KopoBoard.Size);
        Assert.True(KopoBoard.IsPlayable(0, 1));
        Assert.False(KopoBoard.IsPlayable(0, 0));
        Assert.False(KopoBoard.IsPlayable(9, 9));
        Assert.Equal(-1, KopoBoard.ForwardRowDelta(_creator, _creator));
        Assert.Equal(1, KopoBoard.ForwardRowDelta(_opponent, _creator));
        Assert.Equal(0, KopoBoard.KingRow(_creator, _creator));
        Assert.Equal(9, KopoBoard.KingRow(_opponent, _creator));
    }

    [Fact]
    public void TryApplyMove_QuietForward_IsLegalForCreatorMan()
    {
        var state = KopoRules.CreateInitial(_creator, _opponent, _creator);
        var man = state.Pieces.First(p => p.OwnerId == _creator && !p.IsKing && p.Row == 6);
        var targetRow = man.Row - 1;
        var targetCol = man.Col + (KopoBoard.IsPlayable(man.Row - 1, man.Col - 1) ? -1 : 1);
        if (!KopoBoard.IsPlayable(targetRow, targetCol))
            targetCol = man.Col + 1;

        var ok = KopoRules.TryApplyMove(state, _creator, _opponent, _creator,
            new[] { (man.Row, man.Col), (targetRow, targetCol) }, out _);
        Assert.True(ok);
        Assert.Equal(targetRow, state.Pieces.First(p => p.Id == man.Id).Row);
        Assert.Equal(_opponent, state.CurrentTurnPlayerId);
    }

    [Fact]
    public void TryApplyMove_WhenCaptureAvailable_RejectsQuietMove()
    {
        var state = new KopoGameState
        {
            CurrentTurnPlayerId = _creator,
            NextPieceId = 4,
            Pieces =
            [
                new KopoPiece { Id = 1, OwnerId = _creator, Row = 4, Col = 3 },
                new KopoPiece { Id = 2, OwnerId = _opponent, Row = 3, Col = 2 },
                new KopoPiece { Id = 3, OwnerId = _creator, Row = 6, Col = 1 }
            ]
        };

        var ok = KopoRules.TryApplyMove(state, _creator, _opponent, _creator,
            new[] { (6, 1), (5, 0) }, out var error);

        Assert.False(ok);
        Assert.Equal(GameMoveErrorCodes.MustCapture, error);
    }

    [Fact]
    public void TryApplyMove_Capture_RemovesJumpedPieceAndSwitchesTurn()
    {
        var state = new KopoGameState
        {
            CurrentTurnPlayerId = _creator,
            NextPieceId = 3,
            Pieces =
            [
                new KopoPiece { Id = 1, OwnerId = _creator, Row = 4, Col = 3 },
                new KopoPiece { Id = 2, OwnerId = _opponent, Row = 3, Col = 2 }
            ]
        };

        var ok = KopoRules.TryApplyMove(state, _creator, _opponent, _creator,
            new[] { (4, 3), (2, 1) }, out _);

        Assert.True(ok);
        Assert.DoesNotContain(state.Pieces, p => p.Id == 2);
        Assert.Equal((2, 1), (state.Pieces.Single(p => p.Id == 1).Row, state.Pieces.Single(p => p.Id == 1).Col));
        Assert.Equal(_opponent, state.CurrentTurnPlayerId);
        Assert.Null(state.ChainPieceId);
    }

    [Fact]
    public void TryApplyMove_PromotesManOnOpponentBackRow()
    {
        var state = new KopoGameState
        {
            CurrentTurnPlayerId = _creator,
            NextPieceId = 2,
            Pieces = [new KopoPiece { Id = 1, OwnerId = _creator, Row = 1, Col = 0 }]
        };

        var ok = KopoRules.TryApplyMove(state, _creator, _opponent, _creator,
            new[] { (1, 0), (0, 1) }, out _);

        Assert.True(ok);
        Assert.True(state.Pieces.Single(p => p.Id == 1).IsKing);
    }

    [Fact]
    public void TryApplyMove_DuringChain_RejectsOtherPiece()
    {
        var state = new KopoGameState
        {
            CurrentTurnPlayerId = _creator,
            ChainPieceId = 1,
            NextPieceId = 4,
            Pieces =
            [
                new KopoPiece { Id = 1, OwnerId = _creator, Row = 4, Col = 3 },
                new KopoPiece { Id = 2, OwnerId = _creator, Row = 6, Col = 1 },
                new KopoPiece { Id = 3, OwnerId = _opponent, Row = 3, Col = 2 }
            ]
        };

        var ok = KopoRules.TryApplyMove(state, _creator, _opponent, _creator,
            new[] { (6, 1), (5, 0) }, out var error);

        Assert.False(ok);
        Assert.Equal(GameMoveErrorCodes.MustContinueChain, error);
    }

    [Fact]
    public void TryApplyMove_WhenSingleCaptureAvailableButDoubleExists_ReturnsMustMaxCapture()
    {
        var state = new KopoGameState
        {
            CurrentTurnPlayerId = _creator,
            NextPieceId = 4,
            Pieces =
            [
                new KopoPiece { Id = 1, OwnerId = _creator, Row = 5, Col = 4 },
                new KopoPiece { Id = 2, OwnerId = _opponent, Row = 4, Col = 3 },
                new KopoPiece { Id = 3, OwnerId = _opponent, Row = 2, Col = 1 }
            ]
        };

        var ok = KopoRules.TryApplyMove(state, _creator, _opponent, _creator,
            new[] { (5, 4), (3, 2) }, out var error);

        Assert.False(ok);
        Assert.Equal(GameMoveErrorCodes.MustMaxCapture, error);
    }

    [Fact]
    public void TryApplyMove_DoubleCapture_TakesBothPieces()
    {
        var state = new KopoGameState
        {
            CurrentTurnPlayerId = _creator,
            NextPieceId = 4,
            Pieces =
            [
                new KopoPiece { Id = 1, OwnerId = _creator, Row = 5, Col = 4 },
                new KopoPiece { Id = 2, OwnerId = _opponent, Row = 4, Col = 3 },
                new KopoPiece { Id = 3, OwnerId = _opponent, Row = 2, Col = 1 }
            ]
        };

        var ok = KopoRules.TryApplyMove(state, _creator, _opponent, _creator,
            new[] { (5, 4), (3, 2), (1, 0) }, out _);

        Assert.True(ok);
        Assert.Single(state.Pieces);
        Assert.Equal((1, 0), (state.Pieces[0].Row, state.Pieces[0].Col));
    }

    [Fact]
    public void TryApplyMove_KingQuietSlide_IsLegal()
    {
        var state = new KopoGameState
        {
            CurrentTurnPlayerId = _creator,
            NextPieceId = 2,
            Pieces = [new KopoPiece { Id = 1, OwnerId = _creator, Row = 5, Col = 0, IsKing = true }]
        };

        var ok = KopoRules.TryApplyMove(state, _creator, _opponent, _creator,
            new[] { (5, 0), (2, 3) }, out _);

        Assert.True(ok);
        Assert.Equal((2, 3), (state.Pieces[0].Row, state.Pieces[0].Col));
    }

    [Fact]
    public void TryApplyMove_KingCapture_JumpsOneEnemyAlongRay()
    {
        var state = new KopoGameState
        {
            CurrentTurnPlayerId = _creator,
            NextPieceId = 3,
            Pieces =
            [
                new KopoPiece { Id = 1, OwnerId = _creator, Row = 5, Col = 2, IsKing = true },
                new KopoPiece { Id = 2, OwnerId = _opponent, Row = 3, Col = 4 }
            ]
        };

        var ok = KopoRules.TryApplyMove(state, _creator, _opponent, _creator,
            new[] { (5, 2), (2, 5) }, out _);

        Assert.True(ok);
        Assert.Single(state.Pieces);
        Assert.Equal((2, 5), (state.Pieces[0].Row, state.Pieces[0].Col));
    }

    [Fact]
    public void CheckOutcome_CreatorWinsWhenOpponentHasNoPieces()
    {
        var state = new KopoGameState
        {
            Pieces = [new KopoPiece { Id = 1, OwnerId = _creator, Row = 5, Col = 4 }]
        };

        var (winner, loser, isDraw) = KopoRules.CheckOutcome(state, _creator, _opponent);

        Assert.Equal(_creator, winner);
        Assert.Equal(_opponent, loser);
        Assert.False(isDraw);
    }

    [Fact]
    public void CheckOutcome_OpponentWinsWhenCreatorHasNoPieces()
    {
        var state = new KopoGameState
        {
            Pieces = [new KopoPiece { Id = 1, OwnerId = _opponent, Row = 4, Col = 3 }]
        };

        var (winner, loser, isDraw) = KopoRules.CheckOutcome(state, _creator, _opponent);

        Assert.Equal(_opponent, winner);
        Assert.Equal(_creator, loser);
        Assert.False(isDraw);
    }

    [Fact]
    public void CheckOutcome_DrawWhenNeitherPlayerCanMove()
    {
        var state = new KopoGameState
        {
            Pieces =
            [
                new KopoPiece { Id = 1, OwnerId = _creator, Row = 0, Col = 1 },
                new KopoPiece { Id = 2, OwnerId = _opponent, Row = 9, Col = 0 }
            ]
        };

        Assert.False(KopoRules.HasAnyLegalMove(state, _creator, _creator));
        Assert.False(KopoRules.HasAnyLegalMove(state, _creator, _opponent));

        var (winner, loser, isDraw) = KopoRules.CheckOutcome(state, _creator, _opponent);

        Assert.Null(winner);
        Assert.Null(loser);
        Assert.True(isDraw);
    }

    [Fact]
    public void CheckOutcome_OpponentWinsWhenCreatorHasNoLegalMove()
    {
        var state = new KopoGameState
        {
            Pieces =
            [
                new KopoPiece { Id = 1, OwnerId = _creator, Row = 0, Col = 1 },
                new KopoPiece { Id = 2, OwnerId = _opponent, Row = 2, Col = 3 }
            ]
        };

        var (winner, loser, isDraw) = KopoRules.CheckOutcome(state, _creator, _opponent);

        Assert.Equal(_opponent, winner);
        Assert.Equal(_creator, loser);
        Assert.False(isDraw);
    }

    [Fact]
    public void CheckOutcome_CreatorWinsWhenOpponentHasNoLegalMove()
    {
        var state = new KopoGameState
        {
            Pieces =
            [
                new KopoPiece { Id = 1, OwnerId = _opponent, Row = 9, Col = 0 },
                new KopoPiece { Id = 2, OwnerId = _creator, Row = 7, Col = 2 }
            ]
        };

        Assert.False(KopoRules.HasAnyLegalMove(state, _creator, _opponent));
        Assert.True(KopoRules.HasAnyLegalMove(state, _creator, _creator));

        var (winner, loser, isDraw) = KopoRules.CheckOutcome(state, _creator, _opponent);

        Assert.Equal(_creator, winner);
        Assert.Equal(_opponent, loser);
        Assert.False(isDraw);
    }

    [Fact]
    public void CheckOutcome_ContinuesWhenBothSidesCanMove()
    {
        var state = new KopoGameState
        {
            Pieces =
            [
                new KopoPiece { Id = 1, OwnerId = _creator, Row = 6, Col = 1 },
                new KopoPiece { Id = 2, OwnerId = _opponent, Row = 3, Col = 2 }
            ]
        };

        var (winner, loser, isDraw) = KopoRules.CheckOutcome(state, _creator, _opponent);

        Assert.Null(winner);
        Assert.Null(loser);
        Assert.False(isDraw);
    }
}
