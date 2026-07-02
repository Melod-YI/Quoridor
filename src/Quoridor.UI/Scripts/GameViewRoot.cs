using Godot;
using Quoridor.Application;
using Quoridor.Domain.Core;
using Quoridor.UI.Logic;
using Environment = Godot.Environment;

namespace Quoridor.UI;

/// <summary>GameView 场景根节点: 装配相机/灯光/环境 + BoardView/PreviewLayerView/HudView,
/// 订阅 MainController.Session 事件并派发到各子视图。OnEvent 仅改视觉, 绝不重入 Submit/Start。</summary>
public partial class GameViewRoot : Node3D
{
    private MainController? _ctrl;
    private BoardView? _board;
    private PreviewLayerView? _preview;
    private HudView? _hud;
    private Camera3D? _cam;
    private DirectionalLight3D? _light;
    private WorldEnvironment? _env;

    public override void _Ready()
    {
        _ctrl = GetNode<MainController>("/root/MainController");
        BuildScene();
        var cfg = _ctrl.Config!;
        _ctrl.StartSession(cfg);
        _board!.Init(_ctrl);
        _preview!.Init(_board.Layout);
        _hud!.Init(SeatMap.ForFirstMove(cfg.FirstMove));

        _board.CellClicked += OnCellClicked;
        _board.SlotHovered += OnSlotHovered;
        _board.SlotClicked += OnSlotClicked;
        _board.SlotCleared += () => _preview.Clear();
        _hud.BackToStartRequested += OnBackToStart;
        _ctrl.Session!.EventOccurred += OnEvent;

        _hud.RefreshTop(_ctrl.Session.State, cfg);
        _board.Render(_ctrl.Session.State);
        _ctrl.Session.Start();   // 若起手为 AI 同步驱动; 子节点已就绪
    }

    public override void _ExitTree()
    {
        if (_ctrl?.Session is { } s) s.EventOccurred -= OnEvent;
        _ctrl?.EndSession();
    }

    private void BuildScene()
    {
        _light = new DirectionalLight3D { ShadowEnabled = true, Rotation = new Vector3(Mathf.DegToRad(-55), Mathf.DegToRad(30), 0) };
        AddChild(_light);
        _env = new WorldEnvironment();
        var e = new Environment();
        e.GlowEnabled = true;
        e.GlowStrength = 0.8f;
        e.TonemapMode = Environment.ToneMapper.Filmic;
        _env.Environment = e;
        AddChild(_env);
        var cfgBoard = _ctrl!.BoardConfig;
        // 透视投影 + 40° 倾角(原正交 55° 太俯视/平); LookAt 瞄准格区域中心(Size/2),
        // 沿 -Z 后退 camHeight/tan(pitch) 使 look ray 过中心。ray-pick 在任意投影下都准。
        float boardCenter = cfgBoard.Size * 1.0f / 2f;
        float pitch = Mathf.DegToRad(40f);
        float camHeight = cfgBoard.Size * 0.8f;
        float zBack = camHeight / Mathf.Tan(pitch);
        _cam = new Camera3D { Projection = Camera3D.ProjectionType.Perspective, Fov = 50f };
        _cam.Position = new Vector3(boardCenter, camHeight, boardCenter + zBack);
        AddChild(_cam);
        _cam.LookAt(new Vector3(boardCenter, 0f, boardCenter), Vector3.Up);

        _board = new BoardView();
        AddChild(_board);
        _preview = new PreviewLayerView();
        AddChild(_preview);
        _hud = new HudView();
        AddChild(_hud);
    }

    private void OnCellClicked(Cell cell)
    {
        GD.Print($"UI click cell {cell}");
        _ctrl!.Session!.Submit(new MovePawnCommand(cell));
    }

    private void OnSlotHovered(SlotId slot)
    {
        var layout = _board!.Layout;
        if (layout.SlotToWall(slot) is not { } wall) return;
        var preview = PreviewService.PoseWall(_ctrl!.Session!.State, wall);
        _preview!.Show(preview, wall);
    }

    private void OnSlotClicked(SlotId slot)
    {
        if (_board!.Layout.SlotToWall(slot) is not { } wall) return;
        GD.Print($"UI click slot {slot} → wall {wall}");
        _ctrl!.Session!.Submit(new PlaceWallCommand(wall));
    }

    private void OnEvent(IGameEvent e)
    {
        // 纪律: 只改视觉, 绝不调 Submit/Start
        switch (e)
        {
            case PawnMoved:
            case WallPlaced:
            case TurnPassed:
                _board!.Render(_ctrl!.Session!.State);
                _hud!.RefreshTop(_ctrl.Session.State, _ctrl.Config!);
                _hud.AppendNotation(e);
                _preview!.Clear();
                break;
            case WallRejected wr:
                _hud!.ShowFooter($"设墙被拒: {RejectReasonText.Of(wr.Reason)}");
                break;
            case MoveRejected mr:
                _hud!.ShowFooter($"走子被拒: {RejectReasonText.Of(mr.Reason)}");
                break;
            case PlayerWon pw:
                _board!.Render(_ctrl!.Session!.State);
                _hud!.ShowWinner(pw.Who);
                break;
        }
    }

    private void OnBackToStart()
    {
        GetTree().ChangeSceneToFile("res://Scenes/StartFrame.tscn");
    }
}
