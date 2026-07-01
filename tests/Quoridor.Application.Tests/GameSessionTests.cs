using System.Collections.Generic;
using System.Collections.Immutable;
using Quoridor.Application.Logging;
using Quoridor.Application.Seats;
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Xunit;

namespace Quoridor.Application.Tests;

public class GameSessionTests
{
    [Fact]
    public void Human_submit_advances_state_and_broadcasts_events()
    {
        var session = NewHumanVsHuman();
        var events = new List<IGameEvent>();
        session.EventOccurred += e => events.Add(e);

        var r = session.Submit(new MovePawnCommand(new Cell(4, 1)));  // P1 北上 e2

        Assert.NotNull(r.State);
        Assert.Equal(new Cell(4, 1), session.State.PawnOf(PlayerId.P1).Pos);
        Assert.Equal(PlayerId.P2, session.State.ActivePlayer);
        Assert.Contains(events, e => e is PawnMoved);
        Assert.Contains(events, e => e is TurnPassed);
    }

    [Fact]
    public void Illegal_submit_rejected_state_unchanged()
    {
        var session = NewHumanVsHuman();
        var before = session.State;
        var events = new List<IGameEvent>();
        session.EventOccurred += e => events.Add(e);

        var r = session.Submit(new MovePawnCommand(new Cell(4, 2)));  // 跨两格非法

        Assert.Null(r.State);
        Assert.Same(before, session.State);  // 状态不变
        Assert.Contains(events, e => e is MoveRejected);
    }

    [Fact]
    public void Submit_when_finished_is_noop()  // 债7: 终局后不再推进/AI 不被调用
    {
        var session = NewHumanVsHuman();
        // 手动把状态置为终局
        var finished = session.State with { Phase = Phase.Finished, Winner = PlayerId.P1 };
        typeof(GameSession).GetProperty(nameof(GameSession.State))!
            .SetValue(session, finished);  // 测试钩子: 强制终局
        var events = new List<IGameEvent>();
        session.EventOccurred += e => events.Add(e);

        session.Submit(new MovePawnCommand(new Cell(4, 1)));

        Assert.True(session.State.IsFinished);
        Assert.Empty(events);  // 终局后不广播
    }

    [Fact]
    public void Ai_seat_auto_drives_after_human_move()
    {
        // P1 人类, P2 AI(Easy)。人类走一手后, AI 应自动跟进一手并回到人类回合。
        var seats = new IPlayer[]
        {
            new HumanPlayer(PlayerId.P1),
            AiPlayerFactory.Create(PlayerId.P2, Difficulty.Easy),
        };
        var session = new GameSession(BoardConfig.Standard, seats);
        var events = new List<IGameEvent>();
        session.EventOccurred += e => events.Add(e);

        session.Start();  // P1 人类 → 不驱动
        Assert.Empty(events);  // 起手人类, 无 AI 事件

        session.Submit(new MovePawnCommand(new Cell(4, 1)));  // P1 走, 然后 P2 AI 自动

        Assert.Contains(events, e => e is PawnMoved pm && pm.Who == PlayerId.P1);
        Assert.Contains(events, e => e is PawnMoved pm && pm.Who == PlayerId.P2);
        Assert.Equal(PlayerId.P1, session.State.ActivePlayer);  // AI 走完回到 P1
        Assert.NotEqual(new Cell(4, 8), session.State.PawnOf(PlayerId.P2).Pos);  // P2 已动
    }

    [Fact]
    public void Ai_vs_ai_kid_easy_terminates_with_winner()
    {
        var seats = new IPlayer[]
        {
            AiPlayerFactory.Create(PlayerId.P1, Difficulty.Easy),
            AiPlayerFactory.Create(PlayerId.P2, Difficulty.Easy),
        };
        var session = new GameSession(BoardConfig.Kid, seats);

        session.Start();

        Assert.True(session.State.IsFinished);
        Assert.NotNull(session.State.Winner);
    }

    [Fact]
    public void Logger_records_submit_lifecycle()
    {
        var cap = new CapturingLogger();
        var session = new GameSession(BoardConfig.Standard, new IPlayer[]
        {
            new HumanPlayer(PlayerId.P1),
            new HumanPlayer(PlayerId.P2),
        }, cap);

        session.Submit(new MovePawnCommand(new Cell(4, 1)));

        Assert.Contains(cap.Messages, m => m.Contains("Submit") && m.Contains("入口"));
        Assert.Contains(cap.Messages, m => m.Contains("应用成功"));
    }

    [Fact]
    public void Event_log_and_export_reflect_played_plies()
    {
        var session = NewHumanVsHuman();
        session.Submit(new MovePawnCommand(new Cell(4, 1)));  // P1 e2
        session.Submit(new MovePawnCommand(new Cell(4, 7)));  // P2 e8

        var notation = session.Export();
        Assert.Equal("1. e2 e8", notation);
    }

    private static GameSession NewHumanVsHuman() => new(BoardConfig.Standard, new IPlayer[]
    {
        new HumanPlayer(PlayerId.P1),
        new HumanPlayer(PlayerId.P2),
    });

    private sealed class CapturingLogger : IAppLogger
    {
        public List<string> Messages { get; } = new();
        public void Log(LogLevel level, string message, params object[] args) => Messages.Add(message);
    }
}
