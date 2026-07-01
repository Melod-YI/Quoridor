namespace Quoridor.Domain.Core;

public sealed record PlayerState(PlayerId Id, Cell Start, GoalEdge Goal, int WallsLeft);
