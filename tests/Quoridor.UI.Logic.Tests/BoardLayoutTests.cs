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

    [Fact]
    public void WallToSlot_returns_near_slot_and_roundtrips_with_SlotToWall()
    {
        var layout = new BoardLayout(BoardConfig.Standard, 1.0f);
        var wall = new WallPos(new Cell(3, 4), WallOrient.Vertical);
        var slot = layout.WallToSlot(wall);
        Assert.Equal(new SlotId(SlotEdge.Vertical, 3, 4), slot);
        Assert.Equal(wall, layout.SlotToWall(slot!.Value));
    }

    [Fact]
    public void CellToWorld_and_WorldToCell_roundtrip()
    {
        var layout = new BoardLayout(BoardConfig.Standard, 1.0f);
        var cell = new Cell(2, 5);
        var (x, y, z) = layout.CellToWorld(cell);
        var back = layout.WorldToCell(x, z);
        Assert.Equal(cell, back);
    }

    [Fact]
    public void CellToWorld_returns_cell_center_not_corner()
    {
        // 格(c,r) 中心 = (c+0.5, (MaxIndex-r)+0.5); 格角/边界会导致点击区与棋子错位半格
        var layout = new BoardLayout(BoardConfig.Standard, 1.0f); // MaxIndex=8
        var (x, _, z) = layout.CellToWorld(new Cell(2, 5));
        Assert.Equal(2.5f, x);
        Assert.Equal(3.5f, z); // (8-5)+0.5
        // 边界点(格角)不归属于该格——Floor 语义, 中心往返一致
        Assert.Equal(new Cell(2, 5), layout.WorldToCell(2.5f, 3.5f));
        Assert.Equal(new Cell(2, 5), layout.WorldToCell(2.9f, 3.1f));
    }

    [Fact]
    public void WorldToCell_out_of_bounds_returns_null()
    {
        var layout = new BoardLayout(BoardConfig.Standard, 1.0f);
        Assert.Null(layout.WorldToCell(-1, 0));
        Assert.Null(layout.WorldToCell(0, 999));
    }

    [Fact]
    public void PickableSlots_count_for_standard()
    {
        var layout = new BoardLayout(BoardConfig.Standard, 1.0f);
        Assert.Equal(128, layout.PickableSlots().Count());
    }
}
