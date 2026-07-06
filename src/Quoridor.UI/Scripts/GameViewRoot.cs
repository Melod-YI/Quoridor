using System.Collections.Concurrent;
using System.Threading.Tasks;
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
    private bool _aiThinking = false;  // AI 后台决策期间 true, 防重入与人类误操作
    private readonly ConcurrentQueue<IGameCommand?> _aiProposals = new();  // 后台→主线程递交通道

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
        _hud.SurrenderRequested += OnSurrender;
        _ctrl.Session!.EventOccurred += OnEvent;

        _hud.RefreshTop(_ctrl.Session.State, cfg);
        _board.Render(_ctrl.Session.State);
        _ctrl.Session.Start();   // autoDriveAi=false: 不自动驱动; 起手若 AI 由 KickAiIfNeeded 后台驱动
        KickAiIfNeeded();  // 设初始状态提示(人类回合→ShowHumanTurn, AI 回合→思考中)
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
        // 透视投影 + 60° 俯角(从水平面算; 0°平视, 90°正俯视)。原 40° 太斜, 60° 介于垂直与
        // 斜视之间。俯角大→棋盘投影变高, 同步拉远 camHeight=size*1.15 避免裁切。LookAt 瞄准中心。
        float boardCenter = cfgBoard.Size * 1.0f / 2f;
        float pitch = Mathf.DegToRad(60f);
        float camHeight = cfgBoard.Size * 1.15f;
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
            case PlayerSurrendered ps:
                _hud!.ShowSurrendered(ps.Who);
                break;
            case PlayerWon pw:
                _board!.Render(_ctrl!.Session!.State);
                _hud!.ShowWinner(pw.Who);
                break;
        }
        // 异步驱动: 成功事件后若轮到 AI, 后台跑决策; 人类回合则确保输入可用 + 提示轮次
        KickAiIfNeeded();
    }

    /// <summary>AI 异步驱动核心: 轮到 AI 时, 在后台线程跑 PeekAiProposal(纯函数, 只读不可变 State),
    /// 完成后入队 _aiProposals, 由 _Process 在主线程消费 Submit——不阻塞渲染循环。人类回合启用输入 + 提示。
    /// _aiThinking 守卫防重入; BoardView.SetInputEnabled 在 AI 思考期间禁人类点击。</summary>
    private void KickAiIfNeeded()
    {
        var s = _ctrl?.Session;
        if (s is null || _board is null || _hud is null) return;
        if (_aiThinking) return;

        if (s.IsAiTurn)
        {
            _aiThinking = true;
            _board.SetInputEnabled(false);
            _board.Render(s.State);
            _hud.ShowAiThinking();
            // 捕获 session; 后台线程只读不可变 State + 调纯函数 ai.Choose, 安全
            var session = s;
            Task.Run(() => session.PeekAiProposal()).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    GD.PushError($"AI 决策异常: {t.Exception?.InnerException?.Message}");
                _aiProposals.Enqueue(t.IsFaulted ? null : t.Result);
            });
        }
        else if (!s.State.IsFinished)
        {
            _board.SetInputEnabled(true);
            _board.Render(s.State);
            _hud.ShowHumanTurn(s.State);  // AI 落子后这里把"思考中"改成"轮到玩家X"
        }
    }

    /// <summary>主线程消费后台 AI 决策: Submit 后 OnEvent→KickAi 形成链式驱动到人类回合或终局。</summary>
    public override void _Process(double delta)
    {
        if (_ctrl?.Session is not { } s) return;
        while (_aiProposals.TryDequeue(out var cmd))
        {
            _aiThinking = false;
            if (cmd is not null && _ctrl?.Session == s)
                s.Submit(cmd);
        }
    }

    private void OnSurrender()
    {
        if (_ctrl?.Session is not { } s) return;
        if (_aiThinking || s.IsAiTurn || s.State.IsFinished) return;  // 仅人类回合可投降
        s.Submit(new SurrenderCommand());
    }

    private void OnBackToStart()
    {
        GetTree().ChangeSceneToFile("res://Scenes/StartFrame.tscn");
    }
}
