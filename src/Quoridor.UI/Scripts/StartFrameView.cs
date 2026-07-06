using Godot;
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Quoridor.UI.Logic;

namespace Quoridor.UI;

public partial class StartFrameView : Control
{
    private OptionButton _variant = new();
    private OptionButton _mode = new();
    private OptionButton _diff = new();
    private OptionButton _first = new();
    private OptionButton _replay = new();
    private Button _start = new() { Text = "开始对局" };

    public override void _Ready()
    {
        _variant.AddItem("标准 9x9", 0);
        _variant.AddItem("Kid 7x7", 1);
        _mode.AddItem("人机", 0);
        _mode.AddItem("双人 hot-seat", 1);
        _mode.AddItem("回放 AI vs AI", 2);
        foreach (var e in ReplayLibrary.All)
            _replay.AddItem($"{e.Name} · P{(int)e.Winner + 1}胜 · {e.Plies}手");
        _replay.Selected = 0;
        _replay.Visible = false;
        _diff.AddItem("简单", (int)Difficulty.Easy);
        _diff.AddItem("中等", (int)Difficulty.Medium);
        _diff.AddItem("困难", (int)Difficulty.Hard);
        _first.AddItem("玩家1 先手", 0);
        _first.AddItem("玩家2 先手", 1);

        int y = 20;
        foreach (var c in new Control[] { _variant, _mode, _diff, _first, _replay, _start })
        { c.Position = new Vector2(40, y); c.Size = new Vector2(360, 30); AddChild(c); y += 50; }

        _mode.ItemSelected += idx =>
        {
            bool vsAi = idx == 0;
            bool hotSeat = idx == 1;
            bool replay = idx == 2;
            _diff.Visible = vsAi || replay;
            _first.Visible = vsAi || hotSeat;
            _variant.Visible = !replay;       // 回放时变体由棋局决定
            _replay.Visible = replay;
        };
        _start.Pressed += OnStart;
    }

    private void OnStart()
    {
        var ctrl = GetNode<MainController>("/root/MainController");
        if (_mode.Selected == 2)
        {
            var entry = ReplayLibrary.All[_replay.Selected];
            var cfg = new GameConfig(entry.Variant, MatchMode.Replay, Difficulty.Easy, PlayerId.P1, entry);
            ctrl.Config = cfg;
            GD.Print($"StartFrame: replay={entry.Name}");
            GetTree().ChangeSceneToFile("res://Scenes/GameView.tscn");
            return;
        }
        var variant = _variant.Selected == 1 ? BoardVariant.Kid : BoardVariant.Standard;
        var mode = _mode.Selected == 1 ? MatchMode.HotSeat : MatchMode.VsAi;
        var diff = (Difficulty)(_diff.Selected == -1 ? 0 : _diff.Selected);
        var first = _first.Selected == 1 ? PlayerId.P2 : PlayerId.P1;
        ctrl.Config = new GameConfig(variant, mode, diff, first);
        GD.Print($"StartFrame: variant={variant} mode={mode} diff={diff} first={first}");
        GetTree().ChangeSceneToFile("res://Scenes/GameView.tscn");
    }
}
