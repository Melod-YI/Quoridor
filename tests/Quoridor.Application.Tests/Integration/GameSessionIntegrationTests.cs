using System.Collections.Generic;
using Quoridor.Application.Seats;
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Xunit;

namespace Quoridor.Application.Tests.Integration;

public class GameSessionIntegrationTests
{
    [Fact]
    public void Full_ai_vs_ai_standard_easy_terminates_with_winner()
    {
        var seats = new IPlayer[]
        {
            AiPlayerFactory.Create(PlayerId.P1, Difficulty.Easy),
            AiPlayerFactory.Create(PlayerId.P2, Difficulty.Easy),
        };
        var session = new GameSession(BoardConfig.Standard, seats);

        session.Start();

        Assert.True(session.State.IsFinished);
        Assert.NotNull(session.State.Winner);
    }

    [Fact]
    public void Medium_self_play_runs_legal_plies_within_cap()  // 债8: Medium 限步覆盖
    {
        var seats = new IPlayer[]
        {
            AiPlayerFactory.Create(PlayerId.P1, Difficulty.Medium),
            AiPlayerFactory.Create(PlayerId.P2, Difficulty.Medium),
        };
        var session = new GameSession(BoardConfig.Kid, seats);
        var rejections = 0;
        session.EventOccurred += e => { if (e is MoveRejected or WallRejected) rejections++; };

        session.Start(maxPlies: 40);  // 限步, 不强制终局(避免 Minimax depth2 全局过慢)

        Assert.Equal(0, rejections);  // 限步内 AI 永不下非法手
        Assert.True(session.State.Pawns.Length == 2);  // 状态结构完好
    }

    [Fact]
    public void Export_import_roundtrip_rebuilds_final_state()
    {
        var seats = new IPlayer[]
        {
            AiPlayerFactory.Create(PlayerId.P1, Difficulty.Easy),
            AiPlayerFactory.Create(PlayerId.P2, Difficulty.Easy),
        };
        var session = new GameSession(BoardConfig.Kid, seats);
        session.Start();
        Assert.True(session.State.IsFinished);

        var notation = session.Export();
        var replay = new ReplayController(BoardConfig.Kid, 2, notation);
        replay.GoTo(replay.Total);

        Assert.Equal(session.State.PawnOf(PlayerId.P1).Pos, replay.Current.PawnOf(PlayerId.P1).Pos);
        Assert.Equal(session.State.PawnOf(PlayerId.P2).Pos, replay.Current.PawnOf(PlayerId.P2).Pos);
        Assert.Equal(session.State.Winner, replay.Current.Winner);
    }

    [Fact]
    public void Human_vs_ai_full_game_human_autoplays_to_completion()
    {
        // 模拟人类用 Greedy 自动决策, 与 AI 对弈到终局, 验证混合座位通道
        var seats = new IPlayer[]
        {
            new AutoplayHuman(PlayerId.P1),
            AiPlayerFactory.Create(PlayerId.P2, Difficulty.Easy),
        };
        var session = new GameSession(BoardConfig.Kid, seats);
        var plies = 0;

        while (!session.State.IsFinished && plies < 300)
        {
            if (session.State.ActivePlayer == PlayerId.P1)
            {
                var cmd = ((AutoplayHuman)seats[0]).NextCommand(session.State);
                session.Submit(cmd!);
            }
            plies++;
        }

        Assert.True(session.State.IsFinished, $"混合对弈 {plies} 手未终止");
    }

    /// <summary>测试用"自动决策人类": 走 Submit 通道但内部用 Greedy 选命令, 模拟人类输入。</summary>
    private sealed class AutoplayHuman : IPlayer
    {
        private readonly GreedyAi _ai = new();
        public PlayerId Id { get; }
        public bool IsHuman => true;

        public AutoplayHuman(PlayerId id) => Id = id;

        public IGameCommand? NextCommand(GameState state) =>
            state.IsFinished ? null : _ai.Choose(state, Difficulty.Easy);

        // IPlayer.ProposeMove 不被 GameSession 用于人类座位(人类走 Submit), 此处仅满足接口
        IGameCommand? IPlayer.ProposeMove(GameState state) => null;
    }
}
