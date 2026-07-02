namespace Quoridor.Domain.Core;

public enum WallOrient { Horizontal, Vertical }
public enum PlayerId { P1, P2, P3, P4 }
public enum GoalEdge { North, South, West, East }
public enum Phase { Running, Finished }
public enum MoveKind { Step, Jump, DiagonalJump }
public enum BoardVariant { Standard, Kid }

public enum RejectReason
{
    NotYourTurn,
    IllegalMove,
    BlockedByWall,
    OffBoard,
    WallOverlap,
    WallPlusIntersection,
    WallOutOfBounds,
    WallBlocksAllPaths,
    NoWallsLeft,
    GameFinished,
}
