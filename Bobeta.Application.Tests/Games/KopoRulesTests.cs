using Bobeta.Application.Games.Kopo;
using Xunit;

namespace Bobeta.Application.Tests.Games;

public class KopoRulesTests
{
    [Fact]
    public void CreateInitial_Places40PiecesOnDarkSquares()
    {
        var creator = Guid.NewGuid();
        var opponent = Guid.NewGuid();
        var state = KopoRules.CreateInitial(creator, opponent, creator);
        Assert.Equal(40, state.Pieces.Count);
        Assert.Equal(20, state.Pieces.Count(p => p.OwnerId == creator));
        Assert.Equal(20, state.Pieces.Count(p => p.OwnerId == opponent));
        Assert.All(state.Pieces, p => Assert.True(KopoBoard.IsPlayable(p.Row, p.Col)));
    }

    [Fact]
    public void TryApplyMove_QuietForward_IsLegalForCreatorMan()
    {
        var creator = Guid.NewGuid();
        var opponent = Guid.NewGuid();
        var state = KopoRules.CreateInitial(creator, opponent, creator);
        var man = state.Pieces.First(p => p.OwnerId == creator && !p.IsKing && p.Row == 6);
        var targetRow = man.Row - 1;
        var targetCol = man.Col + (KopoBoard.IsPlayable(man.Row - 1, man.Col - 1) ? -1 : 1);
        if (!KopoBoard.IsPlayable(targetRow, targetCol))
            targetCol = man.Col + 1;

        var ok = KopoRules.TryApplyMove(state, creator, opponent, creator,
            new[] { (man.Row, man.Col), (targetRow, targetCol) }, out _);
        Assert.True(ok);
        Assert.Equal(targetRow, state.Pieces.First(p => p.Id == man.Id).Row);
    }
}
