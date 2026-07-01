using System.Linq;
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Domain.Tests.AI;

public class AiActionSetTests
{
    [Fact]
    public void Generate_includes_legal_moves_and_walls()
    {
        var state = GameSetup.CreateStandard2P();
        var actions = AiActionSet.Generate(state);

        // 含至少一个走子（P1 北上到 (4,1)）与至少一面墙
        Assert.Contains(actions, a => a is MovePawnCommand m && m.To == new Cell(4, 1));
        Assert.Contains(actions, a => a is PlaceWallCommand);
        Assert.True(actions.Length > 1);
    }

    [Fact]
    public void All_generated_actions_are_legal()
    {
        var state = GameSetup.CreateStandard2P();
        var actions = AiActionSet.Generate(state);
        Assert.NotEmpty(actions);
        foreach (var a in actions)
        {
            var r = RuleEngine.ValidateAndApply(state, a);
            Assert.NotNull(r.State);  // AiActionSet 只产合法命令
        }
    }

    [Fact]
    public void Generate_omits_walls_when_none_left()
    {
        var state = GameSetup.CreateStandard2P();
        var p1 = state.PlayerOf(PlayerId.P1);
        state = state with
        {
            Players = state.Players.Replace(p1, p1 with { WallsLeft = 0 }),
        };
        var actions = AiActionSet.Generate(state);
        Assert.DoesNotContain(actions, a => a is PlaceWallCommand);
        Assert.Contains(actions, a => a is MovePawnCommand);
    }
}
