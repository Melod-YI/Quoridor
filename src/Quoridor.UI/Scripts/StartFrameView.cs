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
    private Button _start = new() { Text = "开始对局" };

    public override void _Ready()
    {
        _variant.AddItem("标准 9x9", 0);
        _variant.AddItem("Kid 7x7", 1);
        _mode.AddItem("人机", 0);
        _mode.AddItem("双人 hot-seat", 1);
        _diff.AddItem("简单", (int)Difficulty.Easy);
        _diff.AddItem("中等", (int)Difficulty.Medium);
        _diff.AddItem("困难", (int)Difficulty.Hard);
        _first.AddItem("玩家1 先手", 0);
        _first.AddItem("玩家2 先手", 1);

        int y = 20;
        foreach (var c in new Control[] { _variant, _mode, _diff, _first, _start })
        { c.Position = new Vector2(40, y); c.Size = new Vector2(220, 30); AddChild(c); y += 50; }

        _mode.ItemSelected += idx => _diff.Visible = idx == 0;
        _start.Pressed += OnStart;
    }

    private void OnStart()
    {
        var ctrl = GetNode<MainController>("/root/MainController");
        var variant = _variant.Selected == 1 ? BoardVariant.Kid : BoardVariant.Standard;
        var mode = _mode.Selected == 1 ? MatchMode.HotSeat : MatchMode.VsAi;
        var diff = (Difficulty)(_diff.Selected == -1 ? 0 : _diff.Selected);
        var first = _first.Selected == 1 ? PlayerId.P2 : PlayerId.P1;
        ctrl.Config = new GameConfig(variant, mode, diff, first);
        GD.Print($"StartFrame: variant={variant} mode={mode} diff={diff} first={first}");
        GetTree().ChangeSceneToFile("res://Scenes/GameView.tscn");
    }
}
