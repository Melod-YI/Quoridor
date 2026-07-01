using Quoridor.Domain.Core;

namespace Quoridor.Domain.Path;

public static class Reachability
{
    public static bool AllPlayersCanReachGoal(GameState state)
    {
        var graph = new BoardGraph(state);
        foreach (var pawn in state.Pawns)
        {
            var goal = state.PlayerOf(pawn.Owner).Goal;
            if (PathFinder.ShortestPath(graph, pawn.Pos, goal).Distance < 0)
                return false;
        }
        return true;
    }
}
