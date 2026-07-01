using Quoridor.Domain.Core;
using Xunit;

namespace Quoridor.Domain.Tests.Core;

public class CellTests
{
    [Fact]
    public void CompareTo_orders_by_col_then_row()
    {
        Assert.True(new Cell(0, 5).CompareTo(new Cell(1, 0)) < 0);
        Assert.True(new Cell(2, 3).CompareTo(new Cell(2, 5)) < 0);
    }

    [Fact]
    public void WallPos_anchor_and_orient_roundtrip()
    {
        var w = new WallPos(new Cell(4, 2), WallOrient.Vertical);
        Assert.Equal(new Cell(4, 2), w.Anchor);
        Assert.Equal(WallOrient.Vertical, w.Orient);
    }

    [Fact]
    public void BoardConfig_standard_and_kid_sizes()
    {
        Assert.Equal(9, BoardConfig.Standard.Size);
        Assert.Equal(7, BoardConfig.Kid.Size);
        Assert.Equal(8, BoardConfig.Standard.MaxIndex);
    }

    [Fact]
    public void WallBudget_per_player()
    {
        Assert.Equal(10, WallBudget.PerPlayer(BoardVariant.Standard, 2));
        Assert.Equal(5, WallBudget.PerPlayer(BoardVariant.Standard, 4));
        Assert.Equal(8, WallBudget.PerPlayer(BoardVariant.Kid, 2));
        Assert.Equal(4, WallBudget.PerPlayer(BoardVariant.Kid, 4));
    }

    [Fact]
    public void Passage_canonical_orders_cells()
    {
        var p = Passage.Between(new Cell(3, 4), new Cell(2, 4));
        Assert.Equal(new Cell(2, 4), p.A);
        Assert.Equal(new Cell(3, 4), p.B);
    }
}
