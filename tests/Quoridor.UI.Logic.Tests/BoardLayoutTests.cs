using Quoridor.Domain.Core;
using Quoridor.UI.Logic;
using Xunit;

namespace Quoridor.UI.Logic.Tests;

public class BoardLayoutTests
{
    [Fact]
    public void Vertical_slot_maps_to_vertical_wall_with_anchor_equal_slot_coord()
    {
        var layout = new BoardLayout(BoardConfig.Standard, 1.0f);
        var wall = layout.SlotToWall(new SlotId(SlotEdge.Vertical, 3, 4));
        Assert.Equal(new WallPos(new Cell(3, 4), WallOrient.Vertical), wall);
    }

    [Fact]
    public void Horizontal_slot_maps_to_horizontal_wall_with_anchor_equal_slot_coord()
    {
        var layout = new BoardLayout(BoardConfig.Standard, 1.0f);
        var wall = layout.SlotToWall(new SlotId(SlotEdge.Horizontal, 2, 5));
        Assert.Equal(new WallPos(new Cell(2, 5), WallOrient.Horizontal), wall);
    }

    [Theory]
    [InlineData(8)]
    public void Topmost_vertical_slot_is_not_pickable(int r)
    {
        var layout = new BoardLayout(BoardConfig.Standard, 1.0f);
        Assert.Null(layout.SlotToWall(new SlotId(SlotEdge.Vertical, 0, r)));
    }

    [Fact]
    public void Rightmost_horizontal_slot_is_not_pickable()
    {
        var layout = new BoardLayout(BoardConfig.Standard, 1.0f);
        Assert.Null(layout.SlotToWall(new SlotId(SlotEdge.Horizontal, 8, 0)));
    }

    [Fact]
    public void Kid_7_topmost_vertical_slot_not_pickable()
    {
        var layout = new BoardLayout(BoardConfig.Kid, 1.0f);
        Assert.Null(layout.SlotToWall(new SlotId(SlotEdge.Vertical, 0, 6)));
        Assert.NotNull(layout.SlotToWall(new SlotId(SlotEdge.Vertical, 0, 5)));
    }

    [Fact]
    public void Out_of_bounds_slot_returns_null()
    {
        var layout = new BoardLayout(BoardConfig.Standard, 1.0f);
        Assert.Null(layout.SlotToWall(new SlotId(SlotEdge.Vertical, 8, 0)));
        Assert.Null(layout.SlotToWall(new SlotId(SlotEdge.Horizontal, 0, 8)));
    }
}
