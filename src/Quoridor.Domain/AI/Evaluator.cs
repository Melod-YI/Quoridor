using Quoridor.Domain.Core;
using Quoridor.Domain.Path;

namespace Quoridor.Domain.AI;

public static class Evaluator
{
    public const int WinScore = 100_000;
    private const int Unreachable = 1_000;

    public static int Evaluate(GameState state, PlayerId ai)
    {
        if (state.Winner is { } w)
            return w == ai ? WinScore : -WinScore;

        var graph = new BoardGraph(state);
        int myDist = DistOrUnreachable(graph, state.PawnOf(ai), state.PlayerOf(ai).Goal);

        int oppTotal = 0;
        int oppWallsTotal = 0;
        foreach (var p in state.Players)
        {
            if (p.Id == ai) continue;
            oppTotal += DistOrUnreachable(graph, state.PawnOf(p.Id), p.Goal);
            oppWallsTotal += p.WallsLeft;
        }

        int myWalls = state.PlayerOf(ai).WallsLeft;
        return (oppTotal - myDist) * 10 + (myWalls - oppWallsTotal);
    }

    private static int DistOrUnreachable(BoardGraph g, Pawn pawn, GoalEdge goal)
    {
        int d = PathFinder.ShortestPath(g, pawn.Pos, goal).Distance;
        return d < 0 ? Unreachable : d;
    }
}
