namespace Quoridor.Domain.Core;

public interface IGameEvent;
public sealed record PawnMoved(PlayerId Who, Cell From, Cell To, MoveKind Kind) : IGameEvent;
public sealed record WallPlaced(PlayerId Who, WallPos Wall) : IGameEvent;
public sealed record WallRejected(PlayerId Who, WallPos Wall, RejectReason Reason) : IGameEvent;
public sealed record MoveRejected(PlayerId Who, Cell To, RejectReason Reason) : IGameEvent;
public sealed record PlayerWon(PlayerId Who) : IGameEvent;
public sealed record TurnPassed(PlayerId Next) : IGameEvent;
