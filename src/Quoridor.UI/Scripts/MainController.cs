using Godot;
using Quoridor.Application;
using Quoridor.Domain.Core;
using Quoridor.UI.Logic;

namespace Quoridor.UI;

/// <summary>Autoload: 跨场景持久。持有 GameConfig 与 GameSession(仅 GameView 在场期间)。
/// StartFrame 写 Config; GameView._Ready 调 StartSession 构造并订阅, _ExitTree 调 EndSession。</summary>
public partial class MainController : Node
{
    public GameConfig? Config { get; set; }
    public GameSession? Session { get; private set; }
    public GodotAppLogger Logger { get; } = new();

    public BoardConfig BoardConfig =>
        Config?.Variant == BoardVariant.Kid ? BoardConfig.Kid : BoardConfig.Standard;

    public void StartSession(GameConfig cfg)
    {
        Config = cfg;
        EndSession();
        var seats = SeatsBuilder.Build(cfg);
        var board = cfg.Variant == BoardVariant.Kid ? BoardConfig.Kid : BoardConfig.Standard;
        Session = new GameSession(board, seats, Logger);
        GD.Print($"MainController.StartSession variant={cfg.Variant} mode={cfg.Mode} first={cfg.FirstMove}");
    }

    public void EndSession()
    {
        if (Session is not null)
        {
            GD.Print("MainController.EndSession");
            Session = null;
        }
    }

    public override void _Ready() { }
}
