using Quoridor.Domain.Core;
using Xunit;

namespace Quoridor.Domain.Tests.Core;

public class GameSetupTests
{
    [Fact]
    public void Standard2P_starting_positions_and_walls()
    {
        var s = GameSetup.CreateStandard2P();
        Assert.Equal(2, s.Players.Length);
        Assert.Equal(new Cell(4, 0), s.PawnOf(PlayerId.P1).Pos);
        Assert.Equal(new Cell(4, 8), s.PawnOf(PlayerId.P2).Pos);
        Assert.Equal(GoalEdge.North, s.PlayerOf(PlayerId.P1).Goal);
        Assert.Equal(GoalEdge.South, s.PlayerOf(PlayerId.P2).Goal);
        Assert.Equal(10, s.PlayerOf(PlayerId.P1).WallsLeft);
        Assert.Equal(PlayerId.P1, s.ActivePlayer);
        Assert.Equal(Phase.Running, s.Phase);
        Assert.Null(s.Winner);
    }

    [Fact]
    public void Standard4P_starting_positions()
    {
        var s = GameSetup.CreateStandard4P();
        Assert.Equal(4, s.Players.Length);
        Assert.Equal(new Cell(4, 0), s.PawnOf(PlayerId.P1).Pos);
        Assert.Equal(new Cell(0, 4), s.PawnOf(PlayerId.P2).Pos);
        Assert.Equal(new Cell(4, 8), s.PawnOf(PlayerId.P3).Pos);
        Assert.Equal(new Cell(8, 4), s.PawnOf(PlayerId.P4).Pos);
        Assert.Equal(GoalEdge.East, s.PlayerOf(PlayerId.P2).Goal);
        Assert.Equal(GoalEdge.West, s.PlayerOf(PlayerId.P4).Goal);
        Assert.Equal(5, s.PlayerOf(PlayerId.P1).WallsLeft);
    }

    [Fact]
    public void Kid2P_uses_smaller_board_and_walls()
    {
        var s = GameSetup.CreateKid2P();
        Assert.Equal(7, s.Config.Size);
        Assert.Equal(new Cell(3, 0), s.PawnOf(PlayerId.P1).Pos);
        Assert.Equal(8, s.PlayerOf(PlayerId.P1).WallsLeft);
    }

    [Fact]
    public void Kid4P_walls_per_player_is_four()
    {
        var s = GameSetup.CreateKid4P();
        Assert.Equal(4, s.PlayerOf(PlayerId.P1).WallsLeft);
    }

    [Fact]
    public void GoalChecker_detects_north_and_south()
    {
        var cfg = BoardConfig.Standard;
        Assert.True(GoalChecker.Reached(GoalEdge.North, new Cell(4, 8), cfg));
        Assert.False(GoalChecker.Reached(GoalEdge.North, new Cell(4, 7), cfg));
        Assert.True(GoalChecker.Reached(GoalEdge.South, new Cell(4, 0), cfg));
    }
}
