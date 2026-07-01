namespace Quoridor.Domain.Core;

public readonly record struct WallPos(Cell Anchor, WallOrient Orient);
