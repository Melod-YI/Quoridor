using Godot;
using Quoridor.Domain.Core;
using Quoridor.UI.Logic;

namespace Quoridor.UI;

/// <summary>CanvasLayer HUD: TopBar(回合/模式/先手) + NotationPanel + WallBudgetBar + Footer(提示/回到开始页)。</summary>
public partial class HudView : CanvasLayer
{
    private Label _top = new();
    private Label _status = new();   // 大字号当前状态: 轮到玩家X / AI 思考中 / 胜者
    private RichTextLabel _notation = new();
    private Label _budget = new();
    private Label _footer = new();
    private Button _home = new() { Text = "回到开始页" };
    private Button _surrender = new() { Text = "投降" };
    private SeatMap _seatMap;

    public event Action? BackToStartRequested;
    public event Action? SurrenderRequested;

    private static readonly Color P1Color = new(0.95f, 0.8f, 0.15f);
    private static readonly Color P2Color = new(0.25f, 0.55f, 0.95f);
    private static readonly Color ThinkingColor = new(0.7f, 0.7f, 0.7f);

    public void Init(SeatMap seatMap)
    {
        _seatMap = seatMap;
        // 非交互标签设 Ignore, 鼠标穿透到 3D 棋盘, 不阻挡走子/设墙点击; 仅按钮接收点击。
        _top.MouseFilter = Control.MouseFilterEnum.Ignore;
        _status.MouseFilter = Control.MouseFilterEnum.Ignore;
        _notation.MouseFilter = Control.MouseFilterEnum.Ignore;
        _budget.MouseFilter = Control.MouseFilterEnum.Ignore;
        _footer.MouseFilter = Control.MouseFilterEnum.Ignore;
        _top.Position = new Vector2(10, 10);
        _top.Size = new Vector2(800, 30);
        // 大字号状态行, 显眼提示当前轮次
        _status.Position = new Vector2(10, 44);
        _status.Size = new Vector2(800, 44);
        _status.AddThemeFontSizeOverride("font_size", 28);
        _notation.Position = new Vector2(900, 10);
        _notation.Size = new Vector2(360, 600);
        _notation.BbcodeEnabled = true;
        _budget.Position = new Vector2(10, 760);
        _footer.Position = new Vector2(10, 720);
        _footer.Size = new Vector2(800, 30);
        _home.Position = new Vector2(700, 720);
        _home.Size = new Vector2(130, 40);
        _home.Visible = false;
        _surrender.Position = new Vector2(1100, 720);
        _surrender.Size = new Vector2(120, 40);
        _surrender.Visible = false;  // 仅人类回合显示
        _home.Pressed += () => BackToStartRequested?.Invoke();
        _surrender.Pressed += () => SurrenderRequested?.Invoke();
        AddChild(_top); AddChild(_status); AddChild(_notation); AddChild(_budget);
        AddChild(_footer); AddChild(_home); AddChild(_surrender);
    }

    public void RefreshTop(GameState state, GameConfig cfg)
    {
        int active = _seatMap.ToDisplayNumber(state.ActivePlayer);
        _top.Text = $"回合: 玩家{active} | 模式: {cfg.Mode} | 先手: 玩家{_seatMap.ToDisplayNumber(cfg.FirstMove)}";
        int w1 = state.PlayerOf(_seatMap.FromDisplayNumber(1)).WallsLeft;
        int w2 = state.PlayerOf(_seatMap.FromDisplayNumber(2)).WallsLeft;
        _budget.Text = $"墙数 — 玩家1: {w1}  玩家2: {w2}";
    }

    /// <summary>人类回合: 大字提示"轮到玩家X走棋", 按玩家配色; 投降按钮可见可点。</summary>
    public void ShowHumanTurn(GameState state)
    {
        int n = _seatMap.ToDisplayNumber(state.ActivePlayer);
        _status.Text = $"▼ 轮到 玩家{n} 走棋 ▼";
        _status.AddThemeColorOverride("font_color", n == 1 ? P1Color : P2Color);
        _surrender.Visible = true;
        _surrender.Disabled = false;
    }

    /// <summary>AI 思考中: 灰字提示; 投降按钮禁用(不可点)。</summary>
    public void ShowAiThinking()
    {
        _status.Text = "AI 思考中…";
        _status.AddThemeColorOverride("font_color", ThinkingColor);
        _surrender.Disabled = true;
    }

    public void AppendNotation(IGameEvent e)
    {
        if (e is PawnMoved pm)
            _notation.AppendText($"玩家{_seatMap.ToDisplayNumber(pm.Who)}: {NotationOf(pm.To)}\n");
        else if (e is WallPlaced wp)
            _notation.AppendText($"玩家{_seatMap.ToDisplayNumber(wp.Who)}: 墙{wp.Wall.Anchor.Col},{wp.Wall.Anchor.Row}{(wp.Wall.Orient == WallOrient.Vertical ? "V" : "H")}\n");
    }

    private static string NotationOf(Cell c) => $"{(char)('a' + c.Col)}{c.Row + 1}";

    public void ShowFooter(string text) => _footer.Text = text;

    /// <summary>投降结束: footer 注明投降者; _status 由随后的 PlayerWon→ShowWinner 设。</summary>
    public void ShowSurrendered(PlayerId who)
    {
        int n = _seatMap.ToDisplayNumber(who);
        _footer.Text = $"玩家{n} 投降认输";
    }

    public void ShowWinner(PlayerId winner)
    {
        int n = _seatMap.ToDisplayNumber(winner);
        _status.Text = $"★ 玩家{n} 获胜! ★";
        _status.AddThemeColorOverride("font_color", n == 1 ? P1Color : P2Color);
        _home.Visible = true;
        _surrender.Visible = false;  // 终局无投降
    }

    public void ResetNotation() => _notation.Clear();
}
