using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;
using Quoridor.Application.Seats;
using Xunit;

namespace Quoridor.Application.Tests.Seats;

public class PlayerSeatsTests
{
    [Fact]
    public void Human_player_is_human_and_proposes_null()
    {
        IPlayer p = new HumanPlayer(PlayerId.P1);
        Assert.True(p.IsHuman);
        Assert.Equal(PlayerId.P1, p.Id);
        Assert.Null(p.ProposeMove(GameSetup.CreateStandard2P()));
    }

    [Fact]
    public void Ai_player_is_not_human_and_proposes_legal_command()
    {
        IPlayer p = new AiPlayer(PlayerId.P2, new GreedyAi(), Difficulty.Easy);
        Assert.False(p.IsHuman);
        var state = GameSetup.CreateStandard2P();
        var cmd = p.ProposeMove(state);
        Assert.NotNull(cmd);
        var r = RuleEngine.ValidateAndApply(state, cmd!);
        Assert.NotNull(r.State);  // AI 永不下非法手
    }

    [Fact]
    public void Ai_player_returns_null_on_finished_state()  // 债7 防御
    {
        var finished = GameSetup.CreateStandard2P() with { Phase = Phase.Finished, Winner = PlayerId.P1 };
        IPlayer p = new AiPlayer(PlayerId.P1, new GreedyAi(), Difficulty.Easy);
        Assert.Null(p.ProposeMove(finished));
    }

    [Fact]
    public void Factory_easy_uses_greedy_and_returns_legal_command()  // 债9 映射
    {
        var state = GameSetup.CreateStandard2P();
        foreach (Difficulty d in new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard })
        {
            IPlayer p = AiPlayerFactory.Create(PlayerId.P1, d);
            Assert.False(p.IsHuman);
            var cmd = p.ProposeMove(state);
            Assert.NotNull(cmd);
            Assert.NotNull(RuleEngine.ValidateAndApply(state, cmd!).State);  // 各档均合法
        }
    }

    [Fact]
    public void Factory_easy_advances_on_empty_board()
    {
        // Easy=Greedy: 在空盘上 Greedy 选评估最大的推进手, P1 目标北(row 增) → To.Row>0
        var state = GameSetup.CreateStandard2P();
        IPlayer p = AiPlayerFactory.Create(PlayerId.P1, Difficulty.Easy);
        var cmd = p.ProposeMove(state)!;
        var move = Assert.IsType<MovePawnCommand>(cmd);
        Assert.True(move.To.Row > 0, $"Easy 档应向北推进, 实际 To={move.To}");
    }
}
