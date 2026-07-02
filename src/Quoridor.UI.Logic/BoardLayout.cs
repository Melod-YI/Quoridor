using Quoridor.Domain.Core;

namespace Quoridor.UI.Logic;

/// <summary>棋盘布局与坐标映射。SlotId.Row 采用 Domain 行约定: 0=南/底, 向上递增。
/// 屏幕渲染时 CellToWorld 把 row 翻转为世界 Y。槽坐标与 Domain anchor 同构。</summary>
public sealed class BoardLayout
{
    public BoardConfig Cfg { get; }
    public float CellSize { get; }

    public BoardLayout(BoardConfig cfg, float cellSize)
    {
        Cfg = cfg;
        CellSize = cellSize;
    }

    private int MaxIndex => Cfg.MaxIndex;

    /// <summary>竖向槽(c,r) → WallPos((c,r),Vertical); 横向槽(c,r) → WallPos((c,r),Horizontal)。
    /// 顶排竖槽(r=MaxIndex)/最右列横槽(c=MaxIndex)及越界返回 null。</summary>
    public WallPos? SlotToWall(SlotId slot)
    {
        int max = MaxIndex;
        if (slot.Edge == SlotEdge.Vertical)
        {
            int c = slot.Col, r = slot.Row;
            if (c < 0 || c > max - 1) return null;
            if (r < 0 || r > max - 1) return null;
            return new WallPos(new Cell(c, r), WallOrient.Vertical);
        }
        else
        {
            int c = slot.Col, r = slot.Row;
            if (r < 0 || r > max - 1) return null;
            if (c < 0 || c > max - 1) return null;
            return new WallPos(new Cell(c, r), WallOrient.Horizontal);
        }
    }
}
