namespace Quoridor.Domain.Core;

public static class GoalChecker
{
    public static bool Reached(GoalEdge goal, Cell pos, BoardConfig cfg) => goal switch
    {
        GoalEdge.North => pos.Row == cfg.MaxIndex,
        GoalEdge.South => pos.Row == 0,
        GoalEdge.West => pos.Col == 0,
        GoalEdge.East => pos.Col == cfg.MaxIndex,
        _ => false,
    };
}
