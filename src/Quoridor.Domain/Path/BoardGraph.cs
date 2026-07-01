using System.Collections.Generic;
using System.Collections.Immutable;
using Quoridor.Domain.Core;

namespace Quoridor.Domain.Path;

public sealed class BoardGraph
{
    private readonly HashSet<Passage> _blocked;
    public BoardConfig Config { get; }

    public BoardGraph(GameState state)
    {
        Config = state.Config;
        _blocked = new HashSet<Passage>();
        foreach (var w in state.Walls)
            foreach (var p in EdgesOf(w))
                _blocked.Add(p);
    }

    public bool InBounds(Cell c) =>
        c.Col >= 0 && c.Row >= 0 && c.Col <= Config.MaxIndex && c.Row <= Config.MaxIndex;

    public bool HasWallBetween(Cell a, Cell b) => _blocked.Contains(Passage.Between(a, b));

    public static ImmutableArray<Passage> EdgesOf(WallPos w)
    {
        var (anchor, orient) = w;
        if (orient == WallOrient.Horizontal)
        {
            return ImmutableArray.Create(
                Passage.Between(anchor, new Cell(anchor.Col, anchor.Row + 1)),
                Passage.Between(new Cell(anchor.Col + 1, anchor.Row),
                                new Cell(anchor.Col + 1, anchor.Row + 1)));
        }
        return ImmutableArray.Create(
            Passage.Between(anchor, new Cell(anchor.Col + 1, anchor.Row)),
            Passage.Between(new Cell(anchor.Col, anchor.Row + 1),
                            new Cell(anchor.Col + 1, anchor.Row + 1)));
    }
}
