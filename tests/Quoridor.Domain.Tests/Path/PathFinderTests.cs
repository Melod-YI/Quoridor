using Quoridor.Domain.Core;
using Quoridor.Domain.Path;
using Xunit;

namespace Quoridor.Domain.Tests.Path;

public class PathFinderTests
{
    [Fact]
    public void Empty_board_p1_to_north_distance_is_eight()
    {
        var s = GameSetup.CreateStandard2P();
        var g = new BoardGraph(s);
        var r = PathFinder.ShortestPath(g, s.PawnOf(PlayerId.P1).Pos, GoalEdge.North);
        Assert.Equal(8, r.Distance);
        Assert.Equal(9, r.Path.Length); // 起点 + 8 步
        Assert.Equal(new Cell(4, 8), r.Path[^1]);
    }

    [Fact]
    public void Horizontal_wall_in_front_increases_distance()
    {
        var s = GameSetup.CreateStandard2P() with
        {
            Walls = System.Collections.Immutable.ImmutableArray.Create(
                new WallPos(new Cell(4, 3), WallOrient.Horizontal)),
        };
        var g = new BoardGraph(s);
        var r = PathFinder.ShortestPath(g, new Cell(4, 0), GoalEdge.North);
        // 必须绕路，距离 > 8
        Assert.True(r.Distance > 8);
    }

    [Fact]
    public void Goal_at_start_returns_zero_distance()
    {
        var s = GameSetup.CreateStandard2P();
        var g = new BoardGraph(s);
        var r = PathFinder.ShortestPath(g, new Cell(4, 8), GoalEdge.North);
        Assert.Equal(0, r.Distance);
    }
}
