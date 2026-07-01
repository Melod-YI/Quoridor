using System;
using Quoridor.Domain.Core;

namespace Quoridor.Domain.Path;

public static class Directions
{
    public static readonly Cell North = new(0, 1);
    public static readonly Cell South = new(0, -1);
    public static readonly Cell East = new(1, 0);
    public static readonly Cell West = new(-1, 0);
    public static readonly Cell[] All = { North, South, East, West };

    public static Cell Add(Cell a, Cell d) => new(a.Col + d.Col, a.Row + d.Row);

    public static Cell[] Perpendiculars(Cell dir)
    {
        if (dir == North || dir == South) return new[] { East, West };
        if (dir == East || dir == West) return new[] { North, South };
        throw new ArgumentException("方向必须为正交单位方向", nameof(dir));
    }
}
