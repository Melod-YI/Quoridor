using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Xunit;

namespace Quoridor.Domain.Tests.AI;

public class EvaluatorTests
{
    [Fact]
    public void Initial_2p_is_zero()
    {
        // P1/P2 各 dist=8，墙数相等 → (8-8)*10 + (10-10) = 0
        var state = GameSetup.CreateStandard2P();
        Assert.Equal(0, Evaluator.Evaluate(state, PlayerId.P1));
        Assert.Equal(0, Evaluator.Evaluate(state, PlayerId.P2));
    }

    [Fact]
    public void Closer_p1_scores_higher()
    {
        // P1 放到 (4,4)（距北 4 步），P2 仍在 (4,8)（距南 8 步）
        var state = PlaceP1At(GameSetup.CreateStandard2P(), new Cell(4, 4));
        // (8-4)*10 + (10-10) = 40
        Assert.Equal(40, Evaluator.Evaluate(state, PlayerId.P1));
    }

    [Fact]
    public void Winner_returns_win_score()
    {
        var state = GameSetup.CreateStandard2P() with
        {
            Phase = Phase.Finished,
            Winner = PlayerId.P1,
        };
        Assert.Equal(Evaluator.WinScore, Evaluator.Evaluate(state, PlayerId.P1));
        Assert.Equal(-Evaluator.WinScore, Evaluator.Evaluate(state, PlayerId.P2));
    }

    private static GameState PlaceP1At(GameState s, Cell c)
    {
        var p1 = s.PawnOf(PlayerId.P1);
        return s with { Pawns = s.Pawns.Replace(p1, p1 with { Pos = c }) };
    }
}
