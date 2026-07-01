using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Domain.Tests.AI;

public class GreedyAiTests
{
    [Fact]
    public void Chooses_advancing_move_on_empty_board()
    {
        var state = GameSetup.CreateStandard2P();
        var cmd = new GreedyAi().Choose(state, Difficulty.Easy);
        var move = Assert.IsType<MovePawnCommand>(cmd);
        // P1 目标是北（row 增大），贪心选最大评估=向北推进
        Assert.True(move.To.Row > 0, $"期望向北推进，实际 To={move.To}");
    }

    [Fact]
    public void Never_returns_illegal_command()
    {
        var state = GameSetup.CreateStandard2P();
        foreach (Difficulty d in new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard })
        {
            var cmd = new GreedyAi().Choose(state, d);
            var r = RuleEngine.ValidateAndApply(state, cmd);
            Assert.NotNull(r.State);
        }
    }

    [Fact]
    public void Returns_move_when_no_walls_left()
    {
        var state = GameSetup.CreateStandard2P();
        var p1 = state.PlayerOf(PlayerId.P1);
        state = state with
        {
            Players = state.Players.Replace(p1, p1 with { WallsLeft = 0 }),
        };
        var cmd = new GreedyAi().Choose(state, Difficulty.Easy);
        Assert.IsType<MovePawnCommand>(cmd);
    }
}
