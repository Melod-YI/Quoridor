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

    /// <summary>相邻同向墙之间留的视觉缝宽(世界单位)。端对端相邻墙(如横墙 anchor (c,r) 与 (c+2,r))
    /// 中心相距 2 格, 原满长 2 格时首尾相接无缝看似一面长 4 墙, 看不出分界与可插竖墙交点;
    /// 渲染长度取 WallVisualLength 后两端各缩 WallSeamGap/2, 两墙间即留此缝。</summary>
    public const float WallSeamGap = 0.3f;

    /// <summary>墙可视长度 = 2 格 - 缝。中心仍锚 WallCenter, 缩短量对称分摊两端, 故单面墙也略短于 2 格。
    /// BoardView.SyncWalls 与 PreviewLayerView.Show 的长轴统一取此值, 避免两处各算漂移。</summary>
    public float WallVisualLength => CellSize * 2f - WallSeamGap;

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

    /// <summary>墙(2 格长)中心世界坐标。水平墙 anchor(c,r) 在 row r/r+1 间横槽、跨 col c,c+1;
    /// 垂直墙 anchor(c,r) 在 col c/c+1 间竖槽、跨 row r,r+1。两者中心同为格交点
    /// ((c+1)*s, 0, (MaxIndex-r)*s)。集中此处避免渲染/拾取/预览三处各自算偏。</summary>
    public (float X, float Y, float Z) WallCenter(WallPos wall)
    {
        var anchor = wall.Anchor;
        float x = (anchor.Col + 1f) * CellSize;
        float z = (MaxIndex - anchor.Row) * CellSize;
        return (x, 0f, z);
    }

    /// <summary>槽拾取区中心(1 格宽, 居于墙"起始格"的近边): 横向槽(c,r)→格(c,r) 上沿中点;
    /// 竖向槽(c,r)→格(c,r) 右沿中点。跨 1 格使相邻槽相切不重叠——避免 2 格宽拾取区在
    /// 同一格上沿完全重叠, 致同一悬停点同时落入两槽(预览墙取决于鼠标进入方向, 表现为左右抖动)。</summary>
    public (float X, float Y, float Z) SlotPickCenter(SlotId slot)
    {
        float s = CellSize;
        if (slot.Edge == SlotEdge.Horizontal)
        {
            // 锚格(c,r) 上沿(groove between row r and r+1): X=格中心, Z=groove
            float x = (slot.Col + 0.5f) * s;
            float z = (MaxIndex - slot.Row) * s;
            return (x, 0f, z);
        }
        else
        {
            // 锚格(c,r) 右沿(groove between col c and c+1): X=groove, Z=格中心
            float x = (slot.Col + 1f) * s;
            float z = (MaxIndex - slot.Row + 0.5f) * s;
            return (x, 0f, z);
        }
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
