using System.Text;
using Quoridor.Application;
using Quoridor.Application.Seats;
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Quoridor.Domain.Notation;

namespace Quoridor.Demo;

/// <summary>
/// Quoridor 后端逻辑可视验收工具: AI 自对弈, 每步打印 ASCII 棋盘, 终局打印胜者与记谱。
/// 用法: dotnet run -- [P1难度] [P2难度] [--watch]   难度 easy|medium|hard, 默认 easy easy, Kid 7×7。
/// --watch: 每步清屏 + 400ms 延迟, 动画观看; 不加则全量打印便于重定向到文件。
/// </summary>
internal static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length > 0 && args[0].Equals("--gen-replays", StringComparison.OrdinalIgnoreCase))
        {
            GenReplays();
            return;
        }

        bool watch = false;
        var diffs = new List<Difficulty>();
        foreach (var a in args)
        {
            if (a.Equals("--watch", StringComparison.OrdinalIgnoreCase)) { watch = true; continue; }
            if (Enum.TryParse<Difficulty>(a, ignoreCase: true, out var d)) diffs.Add(d);
        }
        Difficulty p1 = diffs.Count > 0 ? diffs[0] : Difficulty.Easy;
        Difficulty p2 = diffs.Count > 1 ? diffs[1] : Difficulty.Easy;
        var board = BoardConfig.Kid;  // 7×7, 16 墙; 改 BoardConfig.Standard 为 9×9 标准版

        var seats = new IPlayer[]
        {
            AiPlayerFactory.Create(PlayerId.P1, p1),
            AiPlayerFactory.Create(PlayerId.P2, p2),
        };
        var session = new GameSession(board, seats);

        int ply = 0;
        session.EventOccurred += e =>
        {
            switch (e)
            {
                case PawnMoved pm:
                    ply++;
                    PrintPly(watch, session.State,
                        $"第 {ply,2} 手  P{(int)pm.Who + 1}  {NotationService.CellToNotation(pm.To),4}  {KindLabel(pm.Kind)}   剩余墙 {WallSummary(session.State)}");
                    break;
                case WallPlaced wp:
                    ply++;
                    PrintPly(watch, session.State,
                        $"第 {ply,2} 手  P{(int)wp.Who + 1}  {NotationService.WallToNotation(wp.Wall),4}  设墙     剩余墙 {WallSummary(session.State)}");
                    break;
                case PlayerWon pw:
                    if (watch) Console.Clear();
                    Console.WriteLine();
                    Console.WriteLine($"★ P{(int)pw.Who + 1} 到达目标边, 获胜!");
                    break;
            }
        };

        Console.WriteLine($"Quoridor 自对弈 Demo   棋盘={board.Variant}({board.Size}×{board.Size})   P1={p1}   P2={p2}   {(watch ? "动画模式" : "全量打印")}");
        Console.WriteLine("棋子 1/2 = P1/P2,  '=' 水平墙,  '|' 垂直墙,  '·' 空格");
        Console.WriteLine();
        Console.WriteLine("初始局面:");
        Console.Write(RenderBoard(session.State));

        session.Start();

        Console.WriteLine();
        Console.WriteLine("──────── 终局 ────────");
        Console.WriteLine($"胜者: P{(session.State.Winner is { } w ? (int)w + 1 : 0)}");
        Console.WriteLine($"总手数: {ply}");
        Console.WriteLine($"记谱: {session.Export()}");
    }

    private static void GenReplays()
    {
        var variants = new[] { BoardVariant.Standard, BoardVariant.Kid };
        var diffs = new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard };
        var entries = new List<(string Name, BoardVariant V, Difficulty P1, Difficulty P2, PlayerId W, int Plies, string Notation)>();

        foreach (var v in variants)
        {
            var cfg = v == BoardVariant.Kid ? BoardConfig.Kid : BoardConfig.Standard;
            foreach (var p1 in diffs)
                foreach (var p2 in diffs)
                {
                    var seats = new IPlayer[]
                    {
                        AiPlayerFactory.Create(PlayerId.P1, p1),
                        AiPlayerFactory.Create(PlayerId.P2, p2),
                    };
                    var session = new GameSession(cfg, seats);  // autoDriveAi=true 默认, 同步驱动
                    session.Start();
                    var notation = session.Export();
                    var winner = session.State.Winner!.Value;
                    int plies = session.EventLog.Count(e => e is PawnMoved or WallPlaced);
                    entries.Add(($"{v} · {p1} vs {p2}", v, p1, p2, winner, plies, notation));
                    Console.WriteLine($"生成 {v} {p1} vs {p2} ... {plies}手 胜={winner}");
                }
        }
        WriteReplayLibrary(entries);
        Console.WriteLine($"已写 {entries.Count} 条到 ReplayLibrary.cs");
    }

    private static void WriteReplayLibrary(
        List<(string Name, BoardVariant V, Difficulty P1, Difficulty P2, PlayerId W, int Plies, string Notation)> entries)
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        // worktree 的 .git 是文件(非目录), 两种都检查, 否则会向上找到主仓库写错位置
        while (dir != null && !dir.GetDirectories(".git").Any() && !dir.GetFiles(".git").Any()) dir = dir.Parent;
        if (dir is null) throw new InvalidOperationException("找不到 git 仓库根");
        string path = Path.Combine(dir.FullName, "src", "Quoridor.UI.Logic", "ReplayLibrary.cs");

        var sb = new StringBuilder();
        sb.AppendLine("using Quoridor.Domain.AI;");
        sb.AppendLine("using Quoridor.Domain.Core;");
        sb.AppendLine();
        sb.AppendLine("namespace Quoridor.UI.Logic;");
        sb.AppendLine();
        sb.AppendLine("public sealed record ReplayEntry(string Name, BoardVariant Variant, Difficulty P1Diff, Difficulty P2Diff, PlayerId Winner, int Plies, string Notation);");
        sb.AppendLine();
        sb.AppendLine("public static class ReplayLibrary");
        sb.AppendLine("{");
        sb.AppendLine("    public static IReadOnlyList<ReplayEntry> All { get; } = new[]");
        sb.AppendLine("    {");
        foreach (var e in entries)
        {
            string safe = e.Notation.Replace("\\", "\\\\").Replace("\"", "\\\"");
            sb.AppendLine($"        new ReplayEntry(\"{e.Name}\", BoardVariant.{e.V}, Difficulty.{e.P1}, Difficulty.{e.P2}, PlayerId.{e.W}, {e.Plies}, \"{safe}\"),");
        }
        sb.AppendLine("    };");
        sb.AppendLine("}");
        File.WriteAllText(path, sb.ToString());
    }

    private static void PrintPly(bool watch, GameState state, string header)
    {
        if (watch)
        {
            Console.Clear();
            Console.WriteLine(header);
            Console.Write(RenderBoard(state));
            Thread.Sleep(400);
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine(header);
            Console.Write(RenderBoard(state));
        }
    }

    private static string KindLabel(MoveKind k) => k switch
    {
        MoveKind.Step => "走子",
        MoveKind.Jump => "直跳",
        MoveKind.DiagonalJump => "斜跳",
        _ => k.ToString(),
    };

    private static string WallSummary(GameState s) =>
        string.Join('/', s.Players.Select(p => p.WallsLeft));

    /// <summary>ASCII 棋盘: 格子=·/棋子, 水平墙='=', 垂直墙='|', 角='+'. 左侧行号 1..N, 底部列字母 a..</summary>
    private static string RenderBoard(GameState s)
    {
        int n = s.Config.Size;
        int max = s.Config.MaxIndex;
        int rows = 2 * n + 1;
        var grid = new char[rows, rows];
        for (int gr = 0; gr < rows; gr++)
            for (int gc = 0; gc < rows; gc++)
            {
                if (gr % 2 == 0 && gc % 2 == 0) grid[gr, gc] = '+';
                else if (gr % 2 == 0) grid[gr, gc] = ' ';
                else if (gc % 2 == 0) grid[gr, gc] = ' ';
                else grid[gr, gc] = '·';
            }

        foreach (var w in s.Walls)
        {
            int c = w.Anchor.Col, r = w.Anchor.Row;
            int grCellR = 2 * (max - r) + 1;  // cell row r 的网格行
            int gcCellC = 2 * c + 1;           // cell col c 的网格列
            if (w.Orient == WallOrient.Horizontal)
            {
                int grooveRow = grCellR - 1;   // r 与 r+1 之间的水平凹槽
                for (int gc = gcCellC; gc <= gcCellC + 2; gc++) grid[grooveRow, gc] = '=';
            }
            else
            {
                int grooveCol = gcCellC + 1;   // c 与 c+1 之间的垂直凹槽
                for (int gr = grCellR - 2; gr <= grCellR; gr++) grid[gr, grooveCol] = '|';
            }
        }

        foreach (var p in s.Pawns)
        {
            int gr = 2 * (max - p.Pos.Row) + 1;
            int gc = 2 * p.Pos.Col + 1;
            grid[gr, gc] = (char)('1' + (int)p.Owner);
        }

        var sb = new StringBuilder();
        for (int gr = 0; gr < rows; gr++)
        {
            if (gr % 2 == 1)
            {
                int r = max - (gr - 1) / 2;
                sb.Append((r + 1).ToString().PadLeft(2)).Append(' ');
            }
            else sb.Append("   ");
            for (int gc = 0; gc < rows; gc++) sb.Append(grid[gr, gc]);
            sb.AppendLine();
        }
        sb.Append("   ");
        for (int gc = 0; gc < rows; gc++)
            sb.Append(gc % 2 == 1 ? (char)('a' + (gc - 1) / 2) : ' ');
        sb.AppendLine();
        return sb.ToString();
    }
}
