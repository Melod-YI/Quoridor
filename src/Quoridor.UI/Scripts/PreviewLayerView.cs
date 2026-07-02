using System.Collections.Generic;
using Godot;
using Quoridor.Application;
using Quoridor.Domain.Core;
using Quoridor.UI.Logic;

namespace Quoridor.UI;

/// <summary>悬浮预览: 候选墙半透明立柱(合法绿/非法红) + 各棋子最短路线(ImmediateMesh) + 步数(Label3D)。
/// Show(preview) 全量重建; Clear() 清空。</summary>
public partial class PreviewLayerView : Node3D
{
    private BoardLayout? _layout;
    private ImmediateMesh? _routeMesh;
    private MeshInstance3D? _routeInst;
    private readonly List<Label3D> _stepLabels = new();
    private MeshInstance3D? _candidateWall;

    private static readonly StandardMaterial3D _lineMat = new()
    { AlbedoColor = new Color(0.2f, 0.9f, 0.3f, 0.9f), NoDepthTest = true };
    private static readonly StandardMaterial3D _legalMat = new()
    { AlbedoColor = new Color(0.2f, 0.9f, 0.3f, 0.4f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha };
    private static readonly StandardMaterial3D _illegalMat = new()
    { AlbedoColor = new Color(0.9f, 0.2f, 0.2f, 0.4f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha };

    public void Init(BoardLayout layout)
    {
        _layout = layout;
        _routeInst = new MeshInstance3D();
        _routeMesh = new ImmediateMesh();
        _routeInst.Mesh = _routeMesh;
        _routeInst.MaterialOverride = _lineMat;
        AddChild(_routeInst);
    }

    public void Show(PreviewResult preview, WallPos wall)
    {
        Clear();
        // 候选墙
        _candidateWall = new MeshInstance3D { Mesh = new BoxMesh() };
        var anchor = wall.Anchor;
        float cx = (anchor.Col + 0.5f) * _layout!.CellSize;
        float cz = (_layout.Cfg.MaxIndex - (anchor.Row + 0.5f)) * _layout.CellSize;
        bool vertical = wall.Orient == WallOrient.Vertical;
        _candidateWall.Scale = new Vector3(vertical ? 0.12f : _layout.CellSize * 2f, 0.6f, vertical ? _layout.CellSize * 2f : 0.12f);
        _candidateWall.Position = new Vector3(cx, 0.3f, cz);
        _candidateWall.MaterialOverride = preview.Legal ? _legalMat : _illegalMat;
        AddChild(_candidateWall);

        if (!preview.Legal) return;

        // 路线 + 步数
        _routeMesh!.ClearSurfaces();
        _routeMesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);
        foreach (var route in preview.Routes)
        {
            foreach (var cell in route.Path)
            {
                var (x, _, z) = _layout.CellToWorld(cell);
                _routeMesh.SurfaceAddVertex(new Vector3(x, 0.05f, z));
            }
            // 值元组不支持 with 表达式, 先解构再构造 Vector3
            var (lx, _, lz) = _layout.CellToWorld(route.Path[0]);
            var label = new Label3D { Text = $"{route.Steps}", Position = new Vector3(lx, 0.6f, lz) };
            label.FontSize = 32;
            AddChild(label);
            _stepLabels.Add(label);
        }
        _routeMesh.SurfaceEnd();
    }

    public void Clear()
    {
        if (_candidateWall is not null) { _candidateWall.QueueFree(); _candidateWall = null; }
        foreach (var l in _stepLabels) l.QueueFree();
        _stepLabels.Clear();
        _routeMesh?.ClearSurfaces();
    }
}
