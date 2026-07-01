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

    public static ImmutableArray<IGameCommand> Decode(string notation) => DecodeCore(notation, null);

    public static ImmutableArray<IGameCommand> Decode(string notation, BoardConfig cfg) => DecodeCore(notation, cfg);

    private static ImmutableArray<IGameCommand> DecodeCore(string notation, BoardConfig? cfg)
    {
        var cmds = new List<IGameCommand>();
        var tokens = notation.Split(new[] { ' ', '\t', '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries);
        foreach (var raw in tokens)
        {
            var t = StripMoveNumber(raw);
            if (t.Length == 0) continue;  // 纯回合标记 / 续谱占位(3. / 3...)

            char last = t[^1];
            if (last == 'h' || last == 'v')
            {
                var cell = ParseCell(t[..^1], cfg);
                var orient = last == 'h' ? WallOrient.Horizontal : WallOrient.Vertical;
                cmds.Add(new PlaceWallCommand(new WallPos(cell, orient)));
            }
            else
            {
                cmds.Add(new MovePawnCommand(ParseCell(t, cfg)));
            }
        }
        return cmds.ToImmutableArray();
    }

    /// <summary>剥离前导回合号与点号(含续谱 ... 占位): "3..." → "", "3.e2" → "e2", "e3h" → "e3h"。</summary>
    private static string StripMoveNumber(string raw)
    {
        int i = 0;
        while (i < raw.Length && (char.IsDigit(raw[i]) || raw[i] == '.')) i++;
        return raw[i..];
    }

    public static GameState Replay(BoardConfig cfg, int playerCount, string notation)
    {
        var cmds = Decode(notation, cfg);  // cfg 感知: 越界坐标在此抛精确异常
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

    private static Cell ParseCell(string s, BoardConfig? cfg)
    {
        if (s.Length < 2 || !char.IsLetter(s[0]) || !char.IsDigit(s[1]))
            throw new NotationParseException($"无法解析坐标: {s}");

        int col = char.ToLowerInvariant(s[0]) - 'a';
        int j = 1;
        int row = 0;
        while (j < s.Length && char.IsDigit(s[j])) { row = row * 10 + (s[j] - '0'); j++; }
        row -= 1;

        if (j != s.Length)
            throw new NotationParseException($"坐标含非法字符: {s}");

        if (cfg is not null)
        {
            if (col < 0 || col > cfg.MaxIndex)
                throw new NotationParseException($"坐标列越界: {s} (列 {col}, 允许 0..{cfg.MaxIndex})");
            if (row < 0 || row > cfg.MaxIndex)
                throw new NotationParseException($"坐标行越界: {s} (行 {row}, 允许 0..{cfg.MaxIndex})");
        }
        return new Cell(col, row);
    }
}
