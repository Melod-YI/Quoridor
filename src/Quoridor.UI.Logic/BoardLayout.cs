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

    /// <summary>反向: 取墙的近边槽(竖墙取下槽 r, 横墙取左槽 c), 用于预览叠绘定位。</summary>
    public SlotId? WallToSlot(WallPos wall)
    {
        var (anchor, orient) = wall;
        if (anchor.Col < 0 || anchor.Col > MaxIndex - 1 || anchor.Row < 0 || anchor.Row > MaxIndex - 1)
            return null;
        return orient == WallOrient.Vertical
            ? new SlotId(SlotEdge.Vertical, anchor.Col, anchor.Row)
            : new SlotId(SlotEdge.Horizontal, anchor.Col, anchor.Row);
    }

    /// <summary>格中心世界坐标。格(c,r) 占据世界 [c,c+1]×[z,z+1], 中心 X=(c+0.5)*s,
    /// Z=((MaxIndex-r)+0.5)*s(row 0 在近端/屏幕下方, 向远端递增), Y=0(棋盘表面)。</summary>
    public (float X, float Y, float Z) CellToWorld(Cell c)
    {
        float x = (c.Col + 0.5f) * CellSize;
        float z = ((MaxIndex - c.Row) + 0.5f) * CellSize;
        return (x, 0f, z);
    }

    /// <summary>反向: 由世界坐标取所在格。用 Floor(格内任意点都属于该格, 中心往返一致)。</summary>
    public Cell? WorldToCell(float x, float z)
    {
        int col = (int)MathF.Floor(x / CellSize);
        int rowFromBottom = (int)MathF.Floor(z / CellSize);
        int row = MaxIndex - rowFromBottom;
        if (col < 0 || col > MaxIndex || row < 0 || row > MaxIndex) return null;
        return new Cell(col, row);
    }

    public IEnumerable<SlotId> PickableSlots()
    {
        for (int c = 0; c <= MaxIndex - 1; c++)
            for (int r = 0; r <= MaxIndex - 1; r++)
            {
                yield return new SlotId(SlotEdge.Vertical, c, r);
                yield return new SlotId(SlotEdge.Horizontal, c, r);
            }
    }
}
