using System.Collections.Generic;
using System.Linq;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Domain.Tests.Rules;

public class SurrenderTests
{
    [Fact]
    public void Surrender_finishes_game_with_opponent_winning()
    {
        var state = GameSetup.CreateStandard2P();  // P1 起手

        var r = RuleEngine.ValidateAndApply(state, new SurrenderCommand());

        Assert.NotNull(r.State);
        Assert.True(r.State!.IsFinished);
        Assert.Equal(PlayerId.P2, r.State.Winner);  // P1 投降 → P2 胜
    }

    [Fact]
    public void Surrender_broadcasts_surrendered_and_won_events()
    {
        var state = GameSetup.CreateStandard2P();

        var r = RuleEngine.ValidateAndApply(state, new SurrenderCommand());

        Assert.Contains(r.Events, e => e is PlayerSurrendered s && s.Who == PlayerId.P1);
        Assert.Contains(r.Events, e => e is PlayerWon w && w.Who == PlayerId.P2);
    }

    [Fact]
    public void Surrender_when_P2_turn_makes_P1_win()
    {
        // 走一手让 P2 回合, P2 投降 → P1 胜
        var state = RuleEngine.ValidateAndApply(GameSetup.CreateStandard2P(),
            new MovePawnCommand(new Cell(4, 1))).State!;

        var r = RuleEngine.ValidateAndApply(state, new SurrenderCommand());

        Assert.True(r.State!.IsFinished);
        Assert.Equal(PlayerId.P1, r.State.Winner);
    }

    [Fact]
    public void Surrender_after_finished_is_silent_noop()
    {
        var finished = GameSetup.CreateStandard2P() with { Phase = Phase.Finished, Winner = PlayerId.P1 };

        var r = RuleEngine.ValidateAndApply(finished, new SurrenderCommand());

        Assert.Null(r.State);  // 状态不变
        Assert.Empty(r.Events);  // 无事件
    }
}
