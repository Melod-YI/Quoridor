using Quoridor.Domain.Core;
using Quoridor.Domain.Notation;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Domain.Tests.Cases;

public class FullGameCases
{
    [Fact]
    public void Two_player_sprint_to_win_round_trips()
    {
        // P1 从 (4,0) 北上直冲到 (4,8) 获胜；P2 在顶行 (3,8)<->(4,8) 往复让出冲刺道。
        // i=0..6：P1 走到 (4,1)..(4,7)，与顶行的 P2 不相邻；i=7：P1 走到 (4,8) 时 P2 恰在 (3,8)，(4,8) 空出且无墙 → 落子并触发 PlayerWon，循环 break。
        var s = GameSetup.CreateStandard2P();
        var events = new System.Collections.Generic.List<IGameEvent>();
        var p2Cells = new Cell[]
        {
            new(3, 8), new(4, 8), new(3, 8), new(4, 8),
            new(3, 8), new(4, 8), new(3, 8), new(4, 8),
        };
        for (int i = 0; i < 8; i++)
        {
            s = Step(s, new Cell(4, i + 1), events);   // P1 北上
            if (s.IsFinished) break;                   // P1 到顶，结束
            s = Step(s, p2Cells[i], events);           // P2 顶行往复
        }
        Assert.Equal(Phase.Finished, s.Phase);
        Assert.Equal(PlayerId.P1, s.Winner);
        Assert.Equal(new Cell(4, 8), s.PawnOf(PlayerId.P1).Pos);

        // 记谱往返一致
        var notation = NotationService.Encode(events, 2);
        var replayed = NotationService.Replay(BoardConfig.Standard, 2, notation);
        Assert.Equal(s.PawnOf(PlayerId.P1).Pos, replayed.PawnOf(PlayerId.P1).Pos);
        Assert.Equal(s.Winner, replayed.Winner);
    }

    [Fact]
    public void Four_player_first_round_four_distinct_actors()
    {
        var s = GameSetup.CreateStandard4P();
        var events = new System.Collections.Generic.List<IGameEvent>();
        s = Step(s, new Cell(4, 1), events);
        s = Step(s, new Cell(1, 4), events);
        s = Step(s, new Cell(4, 7), events);
        s = Step(s, new Cell(7, 4), events);
        Assert.Equal("1. e2 b5 e8 h5", NotationService.Encode(events, 4));
    }

    private static GameState Step(GameState s, Cell to, System.Collections.Generic.List<IGameEvent> ev)
    {
        var r = RuleEngine.ValidateAndApply(s, new MovePawnCommand(to));
        Assert.NotNull(r.State);
        ev.AddRange(r.Events);
        return r.State!;
    }
}
