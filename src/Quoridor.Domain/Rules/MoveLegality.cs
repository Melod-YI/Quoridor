using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Quoridor.Domain.Core;
using Quoridor.Domain.Path;

namespace Quoridor.Domain.Rules;

public static class MoveLegality
{
    public static ImmutableArray<Cell> LegalTargets(GameState state)
    {
        var graph = new BoardGraph(state);
        var pawn = state.PawnOf(state.ActivePlayer);
        var result = new List<Cell>();

        foreach (var d in Directions.All)
        {
            var nb = Directions.Add(pawn.Pos, d);
            if (!graph.InBounds(nb)) continue;
            if (graph.HasWallBetween(pawn.Pos, nb)) continue;

            if (OccupantAt(state, nb) is null)
            {
                result.Add(nb); // 普通一步
            }
            else
            {
                var beyond = Directions.Add(nb, d);
                bool canStraight = graph.InBounds(beyond)
                    && !graph.HasWallBetween(nb, beyond)
                    && OccupantAt(state, beyond) is null;
                if (canStraight)
                {
                    result.Add(beyond); // 直跳
                }
                else
                {
                    foreach (var perp in Directions.Perpendiculars(d))
                    {
                        var side = Directions.Add(nb, perp);
                        if (!graph.InBounds(side)) continue;
                        if (graph.HasWallBetween(nb, side)) continue;
                        if (OccupantAt(state, side) is not null) continue;
                        result.Add(side); // 斜跳
                    }
                }
            }
        }
        return result.Distinct().ToImmutableArray();
    }

    private static Pawn? OccupantAt(GameState state, Cell c)
    {
        foreach (var p in state.Pawns)
            if (p.Pos == c) return p;
        return null;
    }
}
