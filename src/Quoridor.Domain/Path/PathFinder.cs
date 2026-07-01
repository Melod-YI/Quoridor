using System.Collections.Generic;
using System.Collections.Immutable;
using Quoridor.Domain.Core;

namespace Quoridor.Domain.Path;

public static class PathFinder
{
    public readonly record struct PathResult(int Distance, ImmutableArray<Cell> Path);

    public static PathResult ShortestPath(BoardGraph graph, Cell start, GoalEdge goal)
    {
        var cfg = graph.Config;
        var prev = new Dictionary<Cell, Cell>();
        var queue = new Queue<Cell>();
        queue.Enqueue(start);
        prev[start] = start;

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            if (GoalChecker.Reached(goal, cur, cfg))
                return BuildResult(prev, start, cur);

            foreach (var d in Directions.All)
            {
                var nb = Directions.Add(cur, d);
                if (!graph.InBounds(nb)) continue;
                if (graph.HasWallBetween(cur, nb)) continue;
                if (prev.ContainsKey(nb)) continue;
                prev[nb] = cur;
                queue.Enqueue(nb);
            }
        }
        return new PathResult(-1, ImmutableArray<Cell>.Empty);
    }

    private static PathResult BuildResult(Dictionary<Cell, Cell> prev, Cell start, Cell end)
    {
        var path = new List<Cell>();
        var c = end;
        int dist = 0;
        while (c != start) { path.Add(c); c = prev[c]; dist++; }
        path.Add(start);
        path.Reverse();
        return new PathResult(dist, path.ToImmutableArray());
    }
}
