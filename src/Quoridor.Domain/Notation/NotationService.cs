using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;

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

    public static ImmutableArray<IGameCommand> Decode(string notation)
    {
        var cmds = new List<IGameCommand>();
        var tokens = notation.Split(new[] { ' ', '\t', '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries);
        foreach (var raw in tokens)
        {
            var t = raw;
            int dot = t.IndexOf('.');
            if (dot >= 0) t = t[(dot + 1)..];
            if (t.Length == 0) continue;

            char last = t[^1];
            if (last == 'h' || last == 'v')
            {
                var cell = ParseCell(t[..^1]);
                var orient = last == 'h' ? WallOrient.Horizontal : WallOrient.Vertical;
                cmds.Add(new PlaceWallCommand(new WallPos(cell, orient)));
            }
            else
            {
                cmds.Add(new MovePawnCommand(ParseCell(t)));
            }
        }
        return cmds.ToImmutableArray();
    }

    public static GameState Replay(BoardConfig cfg, int playerCount, string notation)
    {
        var cmds = Decode(notation);
        var state = GameSetup.Create(cfg, playerCount);
        foreach (var cmd in cmds)
        {
            var r = RuleEngine.ValidateAndApply(state, cmd);
            if (r.State is null)
                throw new NotationParseException($"非法走子: {cmd}");
            state = r.State!;
        }
        return state;
    }

    private static Cell ParseCell(string s)
    {
        if (s.Length < 2 || !char.IsLetter(s[0]) || !char.IsDigit(s[1]))
            throw new NotationParseException($"无法解析坐标: {s}");
        int col = char.ToLowerInvariant(s[0]) - 'a';
        int row = s[1] - '0' - 1;
        return new Cell(col, row);
    }
}
