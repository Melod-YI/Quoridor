using Quoridor.Domain.Core;
using Quoridor.Domain.Path;
using Xunit;

namespace Quoridor.Domain.Tests.Path;

public class BoardGraphTests
{
    [Fact]
    public void Empty_board_has_no_wall_between_adjacent_cells()
    {
        var g = new BoardGraph(GameSetup.CreateStandard2P());
        Assert.False(g.HasWallBetween(new Cell(4, 0), new Cell(4, 1)));
        Assert.True(g.InBounds(new Cell(0, 0)));
        Assert.True(g.InBounds(new Cell(8, 8)));
        Assert.False(g.InBounds(new Cell(9, 0)));
    }

    [Fact]
    public void Horizontal_wall_blocks_vertical_passages()
    {
        // 水平墙 anchor(4,3)：阻断 (4,3)-(4,4) 与 (5,3)-(5,4)
        var s = GameSetup.CreateStandard2P() with
        {
            Walls = System.Collections.Immutable.ImmutableArray.Create(new WallPos(new Cell(4, 3), WallOrient.Horizontal)),
        };
        var g = new BoardGraph(s);
        Assert.True(g.HasWallBetween(new Cell(4, 3), new Cell(4, 4)));
        Assert.True(g.HasWallBetween(new Cell(5, 3), new Cell(5, 4)));
        Assert.False(g.HasWallBetween(new Cell(4, 3), new Cell(3, 3)));
    }

    [Fact]
    public void Vertical_wall_blocks_horizontal_passages()
    {
        var s = GameSetup.CreateStandard2P() with
        {
            Walls = System.Collections.Immutable.ImmutableArray.Create(new WallPos(new Cell(4, 3), WallOrient.Vertical)),
        };
        var g = new BoardGraph(s);
        Assert.True(g.HasWallBetween(new Cell(4, 3), new Cell(5, 3)));
        Assert.True(g.HasWallBetween(new Cell(4, 4), new Cell(5, 4)));
        Assert.False(g.HasWallBetween(new Cell(4, 3), new Cell(4, 4)));
    }

    [Fact]
    public void EdgesOf_horizontal_returns_two_canonical_passages()
    {
        var edges = BoardGraph.EdgesOf(new WallPos(new Cell(4, 3), WallOrient.Horizontal));
        Assert.Contains(new Passage(new Cell(4, 3), new Cell(4, 4)), edges);
        Assert.Contains(new Passage(new Cell(5, 3), new Cell(5, 4)), edges);
    }
}
