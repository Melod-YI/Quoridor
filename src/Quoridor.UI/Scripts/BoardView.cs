using System.Collections.Generic;
using Godot;
using Quoridor.Application;
using Quoridor.Domain.Core;
using Quoridor.UI.Logic;

namespace Quoridor.UI;

/// <summary>3D 棋盘渲染与输入。程序化构建格子/槽/棋子; Render(state) 幂等全量刷新。
/// 输入: Cell 点击走子; WallSlot hover 预览 + 点击设墙。墙结构变更同步执行(Render 由事件回调主线程调用, 非物理回调, 同步安全)。</summary>
public partial class BoardView : Node3D
{
    private MainController? _ctrl;
    private BoardLayout? _layout;
    private readonly Dictionary<Cell, Area3D> _cells = new();
    private readonly Dictionary<SlotId, Area3D> _slots = new();
    private readonly Dictionary<PlayerId, MeshInstance3D> _pawns = new();
    private readonly Dictionary<WallPos, MeshInstance3D> _walls = new();

    private StandardMaterial3D _boardMat = new() { AlbedoColor = new Color(0.85f, 0.78f, 0.6f), Roughness = 0.6f };
    private StandardMaterial3D _wallMat = new() { AlbedoColor = new Color(0.45f, 0.3f, 0.18f), Roughness = 0.5f };
    private StandardMaterial3D _pawn1Mat = new() { AlbedoColor = new Color(0.9f, 0.85f, 0.2f), Roughness = 0.3f, Metallic = 0.2f };
    private StandardMaterial3D _pawn2Mat = new() { AlbedoColor = new Color(0.2f, 0.5f, 0.9f), Roughness = 0.3f, Metallic = 0.2f };
    private StandardMaterial3D _gridMat = new() { AlbedoColor = new Color(0.3f, 0.22f, 0.12f), NoDepthTest = true };

    public BoardLayout Layout => _layout!;
    public event Action<Cell>? CellClicked;
    public event Action<SlotId>? SlotHovered;
    public event Action<SlotId>? SlotClicked;
    public event Action? SlotCleared;

    public void Init(MainController ctrl)
    {
        _ctrl = ctrl;
        var board = ctrl.BoardConfig;
        _layout = new BoardLayout(board, 1.0f);
        BuildBoard(board);
        BuildPawns(ctrl.Session!.State);
    }

    private void BuildBoard(BoardConfig board)
    {
        // 棋盘平板: 居中于格区域 [0,Size]×[0,Size] 的中心 (Size/2)
        var plate = new MeshInstance3D();
        plate.Mesh = new PlaneMesh { Size = new Vector2(board.Size * Layout.CellSize, board.Size * Layout.CellSize) };
        plate.MaterialOverride = _boardMat;
        plate.Position = new Vector3(board.Size * Layout.CellSize / 2f, 0, board.Size * Layout.CellSize / 2f);
        AddChild(plate);

        // 可见网格线(格边界, 0..Size)
        BuildGrid(board);

        // 格子点击区
        for (int r = 0; r <= board.MaxIndex; r++)
            for (int c = 0; c <= board.MaxIndex; c++)
            {
                var cell = new Cell(c, r);
                var area = MakePickArea(Layout.CellToWorld(cell), new Vector3(Layout.CellSize, 0.02f, Layout.CellSize));
                area.InputEvent += (Node cam, InputEvent ev, Vector3 pos, Vector3 normal, long shape) => OnCellInput(ev, cell);
                _cells[cell] = area;
                AddChild(area);
            }

        // 槽位(只建可触发的)
        foreach (var slot in Layout.PickableSlots())
        {
            var wall = Layout.SlotToWall(slot)!.Value;
            var area = MakeSlotArea(slot, wall);
            area.MouseEntered += () => SlotHovered?.Invoke(slot);
            area.MouseExited += () => SlotCleared?.Invoke();
            area.InputEvent += (Node cam, InputEvent ev, Vector3 pos, Vector3 normal, long shape) => OnSlotInput(ev, slot);
            _slots[slot] = area;
            AddChild(area);
        }
    }

    /// <summary>画格边界网格线(Size+1 条竖线 + Size+1 条横线), 略高于平板避免 z-fight。</summary>
    private void BuildGrid(BoardConfig board)
    {
        var mesh = new ImmediateMesh();
        var inst = new MeshInstance3D { Mesh = mesh };
        AddChild(inst);
        float s = Layout.CellSize;
        float n = board.Size;            // 格数; 边界线在 0..n
        const float y = 0.02f;
        mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        for (int i = 0; i <= (int)n; i++)
        {
            float v = i * s;
            mesh.SurfaceAddVertex(new Vector3(v, y, 0));
            mesh.SurfaceAddVertex(new Vector3(v, y, n * s));
            mesh.SurfaceAddVertex(new Vector3(0, y, v));
            mesh.SurfaceAddVertex(new Vector3(n * s, y, v));
        }
        mesh.SurfaceEnd();
        mesh.SurfaceSetMaterial(0, _gridMat);
    }

    private Area3D MakePickArea((float X, float Y, float Z) pos, Vector3 size)
    {
        var area = new Area3D();
        var col = new CollisionShape3D();
        var box = new BoxShape3D { Size = size };
        col.Shape = box;
        area.AddChild(col);
        area.Position = new Vector3(pos.X, pos.Y, pos.Z);
        area.InputRayPickable = true;
        return area;
    }

    private Area3D MakeSlotArea(SlotId slot, WallPos wall)
    {
        // 槽拾取区放在墙将出现的格交点上(WallCenter), 沿墙方向跨 2 格; 粗 0.3 便于悬停。
        var (cx, _, cz) = Layout.WallCenter(wall);
        const float thick = 0.3f;
        Vector3 size = slot.Edge == SlotEdge.Vertical
            ? new Vector3(thick, 0.4f, Layout.CellSize * 2f)
            : new Vector3(Layout.CellSize * 2f, 0.4f, thick);
        return MakePickArea((cx, 0.2f, cz), size);
    }

    private void BuildPawns(GameState state)
    {
        foreach (var pawn in state.Pawns)
        {
            var mesh = new MeshInstance3D();
            mesh.Mesh = new CylinderMesh { TopRadius = 0.25f, BottomRadius = 0.25f, Height = 0.5f };
            mesh.MaterialOverride = pawn.Owner == PlayerId.P1 ? _pawn1Mat : _pawn2Mat;
            AddChild(mesh);
            _pawns[pawn.Owner] = mesh;
        }
        Render(state);
    }

    /// <summary>幂等全量刷新。基于最新 State 重算棋子位置/墙集合/输入开关。</summary>
    public void Render(GameState state)
    {
        // 棋子位置
        foreach (var pawn in state.Pawns)
        {
            if (_pawns.TryGetValue(pawn.Owner, out var m))
            {
                var (x, y, z) = Layout.CellToWorld(pawn.Pos);
                m.Position = new Vector3(x, 0.25f, z);
            }
        }
        // 墙集合对齐(缺的补, 多的删)。Render 由 GameViewRoot 事件回调(主线程)调用, 同步安全;
        // 故不使用 CallDeferred(HashSet<WallPos> 无法 marshal 为 Godot Variant)。
        var desired = new HashSet<WallPos>(state.Walls);
        SyncWalls(desired);
        // 墙数=0 禁用槽拾取
        bool wallable = state.PlayerOf(state.ActivePlayer).WallsLeft > 0 && !state.IsFinished;
        foreach (var kv in _slots) kv.Value.InputRayPickable = wallable;
        // 终局禁用格子
        foreach (var kv in _cells) kv.Value.InputRayPickable = !state.IsFinished;
    }

    private void SyncWalls(HashSet<WallPos> desired)
    {
        // 删除多余的
        var toRemove = new List<WallPos>();
        foreach (var kv in _walls) if (!desired.Contains(kv.Key)) toRemove.Add(kv.Key);
        foreach (var w in toRemove) { _walls[w].QueueFree(); _walls.Remove(w); }
        // 补齐缺失的
        foreach (var w in desired)
        {
            if (_walls.ContainsKey(w)) continue;
            var mesh = new MeshInstance3D();
            mesh.Mesh = new BoxMesh();
            var (cx, _, cz) = Layout.WallCenter(w);
            bool vertical = w.Orient == WallOrient.Vertical;
            const float thick = 0.18f;
            mesh.Scale = new Vector3(vertical ? thick : Layout.CellSize * 2f, 0.6f, vertical ? Layout.CellSize * 2f : thick);
            mesh.Position = new Vector3(cx, 0.3f, cz);
            mesh.MaterialOverride = _wallMat;
            AddChild(mesh);
            _walls[w] = mesh;
        }
    }

    private void OnCellInput(InputEvent ev, Cell cell)
    {
        if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
            CellClicked?.Invoke(cell);
    }

    private void OnSlotInput(InputEvent ev, SlotId slot)
    {
        if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
            SlotClicked?.Invoke(slot);
    }
}
