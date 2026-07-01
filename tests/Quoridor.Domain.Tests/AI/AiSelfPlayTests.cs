using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Domain.Tests.AI;

public class AiSelfPlayTests
{
    [Fact]
    public void Greedy_self_play_terminates_with_winner()
    {
        var state = GameSetup.CreateStandard2P();
        var ai = new GreedyAi();
        int plies = 0;
        while (!state.IsFinished && plies < 400)
        {
            var cmd = ai.Choose(state, Difficulty.Easy);
            var r = RuleEngine.ValidateAndApply(state, cmd);
            Assert.NotNull(r.State);  // AI 永不下非法手
            state = r.State!;
            plies++;
        }
        Assert.True(state.IsFinished, $"Greedy 自对弈 {plies} 手未终止");
        Assert.NotNull(state.Winner);
    }

    [Fact]
    public void Minimax_easy_self_play_terminates_on_kid()
    {
        var state = GameSetup.CreateKid2P();
        var ai = new MinimaxAi();
        int plies = 0;
        while (!state.IsFinished && plies < 300)
        {
            var cmd = ai.Choose(state, Difficulty.Easy);
            var r = RuleEngine.ValidateAndApply(state, cmd);
            Assert.NotNull(r.State);
            state = r.State!;
            plies++;
        }
        Assert.True(state.IsFinished, $"Minimax(Easy) 自对弈 {plies} 手未终止");
        Assert.NotNull(state.Winner);
    }
}
