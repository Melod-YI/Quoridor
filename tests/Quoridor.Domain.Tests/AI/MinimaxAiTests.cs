using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Domain.Tests.AI;

public class MinimaxAiTests
{
    [Fact]
    public void Never_returns_illegal_command()
    {
        var state = GameSetup.CreateStandard2P();
        var cmd = new MinimaxAi().Choose(state, Difficulty.Easy);
        var r = RuleEngine.ValidateAndApply(state, cmd);
        Assert.NotNull(r.State);
    }

    [Fact]
    public void Takes_immediate_win_when_available()
    {
        // 9×9：P1 在 (4,7)，P2 挪到 (3,8) 让出 (4,8)；P1 一步到 (4,8) 即胜
        var state = MoveP1To(MoveP2To(GameSetup.CreateStandard2P(), new Cell(3, 8)), new Cell(4, 7));
        var cmd = new MinimaxAi().Choose(state, Difficulty.Easy);
        var move = Assert.IsType<MovePawnCommand>(cmd);
        Assert.Equal(new Cell(4, 8), move.To);
    }

    [Fact]
    public void Blocks_opponent_one_step_from_win()
    {
        // Kid 7×7：P1 在 (3,3)，P2 在 (3,1)（距南端 row0 一步），P1 回合。
        // 若 P1 推进，P2 下一手到 (3,0) 获胜 → 评估 -WinScore；
        // MinimaxAi(Medium, depth=2) 看到这一点，会选一面阻断 P2 的墙而非推进。
        var state = MoveP1To(MoveP2To(GameSetup.CreateKid2P(), new Cell(3, 1)), new Cell(3, 3));
        var cmd = new MinimaxAi().Choose(state, Difficulty.Medium);
        Assert.IsType<PlaceWallCommand>(cmd);
        // 且该墙合法
        var r = RuleEngine.ValidateAndApply(state, cmd);
        Assert.NotNull(r.State);
    }

    private static GameState MoveP1To(GameState s, Cell c)
    {
        var p1 = s.PawnOf(PlayerId.P1);
        return s with { Pawns = s.Pawns.Replace(p1, p1 with { Pos = c }) };
    }
    private static GameState MoveP2To(GameState s, Cell c)
    {
        var p2 = s.PawnOf(PlayerId.P2);
        return s with { Pawns = s.Pawns.Replace(p2, p2 with { Pos = c }) };
    }
}
