using System.Collections.Generic;
using System.Linq;
using Quoridor.Domain.Core;
using Quoridor.Domain.Path;

namespace Quoridor.Domain.Rules;

public static class WallLegality
{
    public static RejectReason? Validate(GameState state, WallPos wall)
    {
        var cfg = state.Config;
        if (wall.Anchor.Col < 0 || wall.Anchor.Row < 0
            || wall.Anchor.Col >= cfg.MaxIndex || wall.Anchor.Row >= cfg.MaxIndex)
        {
            return RejectReason.WallOutOfBounds;
        }

        var newEdges = BoardGraph.EdgesOf(wall).ToHashSet();
        foreach (var existing in state.Walls)
            foreach (var e in BoardGraph.EdgesOf(existing))
                if (newEdges.Contains(e))
                    return RejectReason.WallOverlap;

        var tentative = state with { Walls = state.Walls.Add(wall) };
        if (!Reachability.AllPlayersCanReachGoal(tentative))
            return RejectReason.WallBlocksAllPaths;

        return null;
    }
}
