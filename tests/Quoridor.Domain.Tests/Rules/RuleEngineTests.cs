using System.Linq;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Domain.Tests.Rules;

public class RuleEngineTests
{
    [Fact]
    public void Legal_step_advances_turn()
    {
        var s = GameSetup.CreateStandard2P();
        var r = RuleEngine.ValidateAndApply(s, new MovePawnCommand(new Cell(4, 1)));
        Assert.NotNull(r.State);
        Assert.Equal(new Cell(4, 1), r.State!.PawnOf(PlayerId.P1).Pos);
        Assert.Equal(PlayerId.P2, r.State.ActivePlayer);
        Assert.Contains(r.Events, e => e is PawnMoved);
        Assert.Contains(r.Events, e => e is TurnPassed);
    }

    [Fact]
    public void Illegal_step_is_rejected_state_unchanged()
    {
        var s = GameSetup.CreateStandard2P();
        var r = RuleEngine.ValidateAndApply(s, new MovePawnCommand(new Cell(4, 2))); // 跨两格非法
        Assert.Null(r.State);
        Assert.Contains(r.Events, e => e is MoveRejected);
    }

    [Fact]
    public void Legal_wall_consumes_one_wall_and_passes_turn()
    {
        var s = GameSetup.CreateStandard2P();
        var r = RuleEngine.ValidateAndApply(s, new PlaceWallCommand(new WallPos(new Cell(0, 0), WallOrient.Horizontal)));
        Assert.NotNull(r.State);
        Assert.Equal(9, r.State!.PlayerOf(PlayerId.P1).WallsLeft);
        Assert.Equal(PlayerId.P2, r.State.ActivePlayer);
        Assert.Contains(r.Events, e => e is WallPlaced);
    }

    [Fact]
    public void Wall_when_none_left_rejected()
    {
        var s = GameSetup.CreateStandard2P();
        // 手动把 P1 墙数置 0
        s = s with
        {
            Players = s.Players.Replace(s.PlayerOf(PlayerId.P1),
                s.PlayerOf(PlayerId.P1) with { WallsLeft = 0 }),
        };
        var r = RuleEngine.ValidateAndApply(s, new PlaceWallCommand(new WallPos(new Cell(0, 0), WallOrient.Horizontal)));
        Assert.Null(r.State);
        Assert.Contains(r.Events, e => e is WallRejected { Reason: RejectReason.NoWallsLeft });
    }

    [Fact]
    public void Reaching_goal_ends_game_with_win()
    {
        // 把 P1 放到 (4,7)，北边目标 row8，一步即胜；同时把 P2 移开 (4,8) 以免占据目标格
        var s = GameSetup.CreateStandard2P();
        s = s with { Pawns = s.Pawns.Replace(s.PawnOf(PlayerId.P1), s.PawnOf(PlayerId.P1) with { Pos = new Cell(4, 7) }) };
        s = s with { Pawns = s.Pawns.Replace(s.PawnOf(PlayerId.P2), s.PawnOf(PlayerId.P2) with { Pos = new Cell(3, 8) }) };
        var r = RuleEngine.ValidateAndApply(s, new MovePawnCommand(new Cell(4, 8)));
        Assert.NotNull(r.State);
        Assert.Equal(Phase.Finished, r.State!.Phase);
        Assert.Equal(PlayerId.P1, r.State.Winner);
        Assert.Contains(r.Events, e => e is PlayerWon);
        Assert.DoesNotContain(r.Events, e => e is TurnPassed);
    }

    [Fact]
    public void Move_after_finished_rejected()
    {
        var finished = GameSetup.CreateStandard2P() with { Phase = Phase.Finished, Winner = PlayerId.P1 };
        var r = RuleEngine.ValidateAndApply(finished, new MovePawnCommand(new Cell(4, 1)));
        Assert.Null(r.State);
        Assert.Contains(r.Events, e => e is MoveRejected { Reason: RejectReason.GameFinished });
    }
}
