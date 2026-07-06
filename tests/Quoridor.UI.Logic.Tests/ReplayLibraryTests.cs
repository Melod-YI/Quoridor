using Quoridor.Application;
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

    [Fact]
    public void All_has_18_entries() => Assert.Equal(18, ReplayLibrary.All.Count);

    [Fact]
    public void Covers_9_diff_pairs_times_2_variants()
    {
        var std = ReplayLibrary.All.Where(e => e.Variant == BoardVariant.Standard).ToList();
        var kid = ReplayLibrary.All.Where(e => e.Variant == BoardVariant.Kid).ToList();
        Assert.Equal(9, std.Count);
        Assert.Equal(9, kid.Count);
        var keys = std.Select(e => (e.P1Diff, e.P2Diff)).ToHashSet();
        Assert.Equal(9, keys.Count);
        var kidKeys = kid.Select(e => (e.P1Diff, e.P2Diff)).ToHashSet();
        Assert.Equal(9, kidKeys.Count);
    }

    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)]
    [InlineData(5)][InlineData(6)][InlineData(7)][InlineData(8)][InlineData(9)]
    [InlineData(10)][InlineData(11)][InlineData(12)][InlineData(13)][InlineData(14)]
    [InlineData(15)][InlineData(16)][InlineData(17)]
    public void Each_replay_decodes_and_plays_to_stated_winner(int i)
    {
        var e = ReplayLibrary.All[i];
        var cfg = e.Variant == BoardVariant.Kid ? BoardConfig.Kid : BoardConfig.Standard;
        var rc = new ReplayController(cfg, 2, e.Notation);
        rc.GoTo(rc.Total);
        Assert.True(rc.Current.IsFinished);
        Assert.Equal(e.Winner, rc.Current.Winner);
        Assert.Equal(e.Plies, rc.Total);
    }
}
