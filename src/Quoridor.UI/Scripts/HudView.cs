using Godot;
using Quoridor.Domain.Core;
using Quoridor.UI.Logic;

namespace Quoridor.UI;

/// <summary>CanvasLayer HUD: TopBar(回合/模式/先手) + NotationPanel + WallBudgetBar + Footer(提示/回到开始页)。</summary>
public partial class HudView : CanvasLayer
{
    private Label _top = new();
    private RichTextLabel _notation = new();
    private Label _budget = new();
    private Label _footer = new();
    private Button _home = new() { Text = "回到开始页" };
    private SeatMap _seatMap;

    public event Action? BackToStartRequested;

    public void Init(SeatMap seatMap)
    {
        _seatMap = seatMap;
        _top.Position = new Vector2(10, 10);
        _top.Size = new Vector2(800, 30);
        _notation.Position = new Vector2(900, 10);
        _notation.Size = new Vector2(360, 600);
        _notation.BbcodeEnabled = true;
        _budget.Position = new Vector2(10, 760);
        _footer.Position = new Vector2(10, 720);
        _footer.Size = new Vector2(800, 30);
        _home.Position = new Vector2(700, 720);
        _home.Visible = false;
        _home.Pressed += () => BackToStartRequested?.Invoke();
        AddChild(_top); AddChild(_notation); AddChild(_budget); AddChild(_footer); AddChild(_home);
    }

    public void RefreshTop(GameState state, GameConfig cfg)
    {
        int active = _seatMap.ToDisplayNumber(state.ActivePlayer);
        _top.Text = $"回合: 玩家{active} | 模式: {cfg.Mode} | 先手: 玩家{_seatMap.ToDisplayNumber(cfg.FirstMove)}";
        int w1 = state.PlayerOf(_seatMap.FromDisplayNumber(1)).WallsLeft;
        int w2 = state.PlayerOf(_seatMap.FromDisplayNumber(2)).WallsLeft;
        _budget.Text = $"墙数 — 玩家1: {w1}  玩家2: {w2}";
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

    public void ShowWinner(PlayerId winner)
    {
        _footer.Text = $"玩家{_seatMap.ToDisplayNumber(winner)} 获胜!";
        _home.Visible = true;
    }

    public void ResetNotation() => _notation.Clear();
}
