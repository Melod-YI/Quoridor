using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Xunit;

namespace Quoridor.Domain.Tests.AI;

public class IQuoridorAiTests
{
    [Fact]
    public void Difficulty_enum_has_three_levels()
    {
        Assert.True(System.Enum.IsDefined(typeof(Difficulty), "Easy"));
        Assert.True(System.Enum.IsDefined(typeof(Difficulty), "Medium"));
        Assert.True(System.Enum.IsDefined(typeof(Difficulty), "Hard"));
    }

    [Fact]
    public void Stub_ai_returns_a_legal_command()
    {
        // 用一个最简 stub 验证接口契约：Choose 返回 IGameCommand
        IQuoridorAi ai = new StubAdvanceAi();
        var cmd = ai.Choose(GameSetup.CreateStandard2P(), Difficulty.Easy);
        Assert.IsType<MovePawnCommand>(cmd);
        Assert.Equal(new Cell(4, 1), ((MovePawnCommand)cmd).To);
    }

    private sealed class StubAdvanceAi : IQuoridorAi
    {
        public IGameCommand Choose(GameState state, Difficulty difficulty) =>
            new MovePawnCommand(new Cell(4, 1));
    }
}
