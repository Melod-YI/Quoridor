using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Quoridor.UI.Logic;
using Xunit;

namespace Quoridor.UI.Logic.Tests;

public class ReplayLibraryTests
{
    [Fact]
    public void GameConfig_replay_mode_carries_entry()
    {
        var entry = new ReplayEntry("t", BoardVariant.Standard, Difficulty.Easy, Difficulty.Easy, PlayerId.P1, 0, "");
        var cfg = new GameConfig(BoardVariant.Standard, MatchMode.Replay, Difficulty.Easy, PlayerId.P1, entry);
        Assert.Equal(MatchMode.Replay, cfg.Mode);
        Assert.Same(entry, cfg.Replay);
    }
}
