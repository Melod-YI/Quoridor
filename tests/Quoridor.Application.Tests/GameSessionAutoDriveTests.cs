using System.Collections.Generic;
using System.Linq;
using Quoridor.Application.Seats;
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Application.Tests;

/// <summary>异步驱动模式(autoDriveAi=false): Godot 端在后台线程跑 PeekAiProposal,
/// 拿到命令后回主线程 Submit。这组测试锁定该契约。</summary>
public class GameSessionAutoDriveTests
{
    [Fact]
    public void AutoDrive_disabled_submit_does_not_drive_ai()
    {
        // P1 人类, P2 AI(Easy)。关掉自动驱动后, 人类走一手, AI 不应自动跟进。
        var seats = new IPlayer[]
        {
            new HumanPlayer(PlayerId.P1),
            AiPlayerFactory.Create(PlayerId.P2, Difficulty.Easy),
        };
        var session = new GameSession(BoardConfig.Standard, seats, autoDriveAi: false);
        var events = new List<IGameEvent>();
        session.EventOccurred += e => events.Add(e);

        session.Submit(new MovePawnCommand(new Cell(4, 1)));  // P1 走

        // 仅 P1 的走子事件 + TurnPassed, 无 P2 的 PawnMoved
        Assert.Contains(events, e => e is PawnMoved pm && pm.Who == PlayerId.P1);
        Assert.DoesNotContain(events, e => e is PawnMoved pm && pm.Who == PlayerId.P2);
        Assert.Equal(PlayerId.P2, session.State.ActivePlayer);  // 仍轮到 P2(AI), 等待手动驱动
        Assert.True(session.IsAiTurn);
    }

    [Fact]
    public void AutoDrive_disabled_start_does_not_drive_ai_when_ai_first()
    {
        // 先手=P2 → P1 座位=AI(先手)。Start 不应驱动。
        var seats = new IPlayer[]
        {
            AiPlayerFactory.Create(PlayerId.P1, Difficulty.Easy),
            new HumanPlayer(PlayerId.P2),
        };
        var session = new GameSession(BoardConfig.Standard, seats, autoDriveAi: false);
        var events = new List<IGameEvent>();
        session.EventOccurred += e => events.Add(e);

        session.Start();

        Assert.Empty(events);  // 未驱动
        Assert.True(session.IsAiTurn);  // 起手仍是 AI, 等待手动驱动
    }

    [Fact]
    public void PeekAiProposal_returns_legal_command_on_ai_turn()
    {
        var seats = new IPlayer[]
        {
            new HumanPlayer(PlayerId.P1),
            AiPlayerFactory.Create(PlayerId.P2, Difficulty.Easy),
        };
        var session = new GameSession(BoardConfig.Standard, seats, autoDriveAi: false);
        session.Submit(new MovePawnCommand(new Cell(4, 1)));  // 轮到 P2 AI

        var cmd = session.PeekAiProposal();

        Assert.NotNull(cmd);
        // 该命令对当前状态必须合法(可应用)
        var r = RuleEngine.ValidateAndApply(session.State, cmd!);
        Assert.NotNull(r.State);
    }

    [Fact]
    public void PeekAiProposal_returns_null_on_human_turn()
    {
        var seats = new IPlayer[]
        {
            new HumanPlayer(PlayerId.P1),
            AiPlayerFactory.Create(PlayerId.P2, Difficulty.Easy),
        };
        var session = new GameSession(BoardConfig.Standard, seats, autoDriveAi: false);

        // 起手 P1 人类
        Assert.False(session.IsAiTurn);
        Assert.Null(session.PeekAiProposal());
    }

    [Fact]
    public void PeekAiProposal_returns_null_when_finished()
    {
        var seats = new IPlayer[]
        {
            new HumanPlayer(PlayerId.P1),
            AiPlayerFactory.Create(PlayerId.P2, Difficulty.Easy),
        };
        var session = new GameSession(BoardConfig.Standard, seats, autoDriveAi: false);
        // 强制终局
        var finished = session.State with { Phase = Phase.Finished, Winner = PlayerId.P1 };
        typeof(GameSession).GetProperty(nameof(GameSession.State))!.SetValue(session, finished);

        Assert.False(session.IsAiTurn);
        Assert.Null(session.PeekAiProposal());
    }

    [Fact]
    public void AutoDrive_disabled_does_not_break_ai_vs_ai_termination_when_explicitly_driven()
    {
        // 关掉自动驱动后, 由调用方循环 PeekAiProposal + Submit 推进, 仍能终局。
        var seats = new IPlayer[]
        {
            AiPlayerFactory.Create(PlayerId.P1, Difficulty.Easy),
            AiPlayerFactory.Create(PlayerId.P2, Difficulty.Easy),
        };
        var session = new GameSession(BoardConfig.Kid, seats, autoDriveAi: false);

        // 不调 Start(它也不驱动); 手动循环驱动到终局
        int guard = 0;
        while (!session.State.IsFinished && guard++ < 2000)
        {
            var cmd = session.PeekAiProposal();
            if (cmd is null) break;
            session.Submit(cmd!);
        }

        Assert.True(session.State.IsFinished);
        Assert.NotNull(session.State.Winner);
    }
}
