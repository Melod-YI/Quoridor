using System.Collections.Generic;
using System.Text;
using Quoridor.Domain.Core;

namespace Quoridor.Domain.Notation;

public static class NotationService
{
    public static string CellToNotation(Cell c) => $"{(char)('a' + c.Col)}{c.Row + 1}";

    public static string WallToNotation(WallPos w) =>
        $"{CellToNotation(w.Anchor)}{(w.Orient == WallOrient.Horizontal ? 'h' : 'v')}";

    public static string Encode(IReadOnlyList<IGameEvent> events, int playerCount)
    {
        var plies = new List<string>();
        foreach (var e in events)
        {
            if (e is PawnMoved pm) plies.Add(CellToNotation(pm.To));
            else if (e is WallPlaced wp) plies.Add(WallToNotation(wp.Wall));
        }

        var sb = new StringBuilder();
        for (int i = 0; i < plies.Count; i++)
        {
            if (i % playerCount == 0)
            {
                if (i > 0) sb.Append(' ');
                sb.Append((i / playerCount) + 1).Append('.');
            }
            sb.Append(' ').Append(plies[i]);
        }
        return sb.ToString().Trim();
    }
}
