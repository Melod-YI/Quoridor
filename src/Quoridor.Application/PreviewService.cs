using System.Collections.Generic;
using System.Collections.Immutable;
using Quoridor.Domain.Core;
using Quoridor.Domain.Path;
using Quoridor.Domain.Rules;

namespace Quoridor.Application;

public readonly record struct RoutePreview(PlayerId Pawn, int Steps, ImmutableArray<Cell> Path);

public readonly record struct PreviewResult(bool Legal, RejectReason? Reason, ImmutableArray<RoutePreview> Routes);

/// <summary>设墙悬浮预览(只读): 不改真实状态, 临时叠加一面墙算各棋子最短路线/步数与合法性。</summary>
public static class PreviewService
{
    public static PreviewResult PoseWall(GameState state, WallPos wall)
    {
        // 合法性: 用 WallLegality(含重叠/越界/可达性, 不含墙数——预览忽略墙数)
        var reason = WallLegality.Validate(state, wall);
        if (reason is not null)
            return new PreviewResult(false, reason, ImmutableArray<RoutePreview>.Empty);

        // 临时叠加墙算路线(不改原 state)
        var previewed = state with { Walls = state.Walls.Add(wall) };
        var graph = new BoardGraph(previewed);

        var routes = new List<RoutePreview>();
        foreach (var pawn in previewed.Pawns)
        {
            var goal = previewed.PlayerOf(pawn.Owner).Goal;
            var pr = PathFinder.ShortestPath(graph, pawn.Pos, goal);
            routes.Add(new RoutePreview(pawn.Owner, pr.Distance, pr.Path));
        }
        return new PreviewResult(true, null, routes.ToImmutableArray());
    }
}
