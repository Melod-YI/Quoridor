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
        {
            foreach (var e in BoardGraph.EdgesOf(existing))
                if (newEdges.Contains(e))
                    return RejectReason.WallOverlap;

            // 禁止 "+" 字交叉: 不同朝向同 anchor 的两面墙都穿过同一格角(plus), 非法。
            // T 字结构(偏移 anchor, 一墙端点落于另一墙穿过点)合法, 不在此列。
            if (existing.Orient != wall.Orient && existing.Anchor == wall.Anchor)
                return RejectReason.WallPlusIntersection;
        }

        var tentative = state with { Walls = state.Walls.Add(wall) };
        if (!Reachability.AllPlayersCanReachGoal(tentative))
            return RejectReason.WallBlocksAllPaths;

        return null;
    }
}
