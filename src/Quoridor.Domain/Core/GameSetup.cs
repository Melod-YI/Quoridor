using System.Collections.Immutable;
using System.Linq;

namespace Quoridor.Domain.Core;

public static class GameSetup
{
    public static GameState CreateStandard2P() => Create(BoardConfig.Standard, 2);
    public static GameState CreateStandard4P() => Create(BoardConfig.Standard, 4);
    public static GameState CreateKid2P() => Create(BoardConfig.Kid, 2);
    public static GameState CreateKid4P() => Create(BoardConfig.Kid, 4);

    public static GameState Create(BoardConfig cfg, int players)
    {
        int mid = cfg.MaxIndex / 2;
        (Cell Start, GoalEdge Goal)[] defs = players switch
        {
            2 => new[] {
                (new Cell(mid, 0), GoalEdge.North),
                (new Cell(mid, cfg.MaxIndex), GoalEdge.South),
            },
            4 => new[] {
                (new Cell(mid, 0), GoalEdge.North),
                (new Cell(0, mid), GoalEdge.East),
                (new Cell(mid, cfg.MaxIndex), GoalEdge.South),
                (new Cell(cfg.MaxIndex, mid), GoalEdge.West),
            },
            _ => throw new System.ArgumentOutOfRangeException(nameof(players)),
        };
        int walls = WallBudget.PerPlayer(cfg.Variant, players);
        var ps = defs
            .Select((d, i) => new PlayerState((PlayerId)i, d.Start, d.Goal, walls))
            .ToImmutableArray();
        var pawns = ps.Select(p => new Pawn(p.Id, p.Start)).ToImmutableArray();
        return new GameState(cfg, ps, pawns, ImmutableArray<WallPos>.Empty,
            PlayerId.P1, Phase.Running, null);
    }
}
