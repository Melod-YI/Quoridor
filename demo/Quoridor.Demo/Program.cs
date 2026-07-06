using System.Text;
using Quoridor.Application;
using Quoridor.Application.Seats;
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Quoridor.Domain.Notation;
using Quoridor.Domain.Rules;
using Quoridor.UI.Logic;

namespace Quoridor.Demo;

/// <summary>
/// Quoridor 后端逻辑可视验收工具: AI 自对弈, 每步打印 ASCII 棋盘, 终局打印胜者与记谱。
/// 用法: dotnet run -- [P1难度] [P2难度] [--watch]   难度 easy|medium|hard, 默认 easy easy, Kid 7×7。
/// --watch: 每步清屏 + 400ms 延迟, 动画观看; 不加则全量打印便于重定向到文件。
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0].Equals("--gen-replays", StringComparison.OrdinalIgnoreCase))
        {
            GenReplays();
            return 0;
        }

        if (args.Length > 0 && args[0].Equals("--acceptance", StringComparison.OrdinalIgnoreCase))
        {
            return RunAcceptance();
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
        return 0;
    }

    private static int RunAcceptance()
    {
        int fails = 0;
        fails += Scenario1Swap();
        fails += Scenario1KidGame();
        fails += Scenario2Preview();
        if (fails > 0)
        {
            Console.WriteLine($"\n验收失败: {fails} 项");
            return 1;
        }
        Console.WriteLine("\n验收全部 PASS");
        return 0;
    }

    private static int Scenario1Swap()
    {
        Console.WriteLine("── 场景 1a: 人机 Kid P2先手换位 ──");
        try
        {
            var cfg = new GameConfig(BoardVariant.Kid, MatchMode.VsAi, Difficulty.Medium, PlayerId.P2);
            var seats = SeatsBuilder.Build(cfg);
            var map = SeatMap.ForFirstMove(PlayerId.P2);

            bool ok = seats[0].Id == PlayerId.P1 && !seats[0].IsHuman   // AI 先手(P1 座位=AI)
                   && seats[1].Id == PlayerId.P2 && seats[1].IsHuman    // 人类后手
                   && map.ToDisplayNumber(PlayerId.P1) == 2              // P1 显作玩家2
                   && map.ToDisplayNumber(PlayerId.P2) == 1;             // P2 显作玩家1

            Console.WriteLine($"  seats[0]={(seats[0].IsHuman ? "人类" : "AI")}-{seats[0].Id}  seats[1]={(seats[1].IsHuman ? "人类" : "AI")}-{seats[1].Id}");
            Console.WriteLine($"  显示映射 P1→玩家{map.ToDisplayNumber(PlayerId.P1)}  P2→玩家{map.ToDisplayNumber(PlayerId.P2)}");

            if (!ok) { Console.WriteLine("  [FAIL] 换位断言不成立"); return 1; }
            Console.WriteLine("  [PASS]");
            return 0;
        }
        catch (Exception ex) { Console.WriteLine($"  [FAIL] 异常: {ex.Message}"); return 1; }
    }

    private static int Scenario1KidGame()
    {
        Console.WriteLine("── 场景 1b: Kid 7×7 AI vs AI 终局流 ──");
        try
        {
            var seats = new IPlayer[]
            {
                AiPlayerFactory.Create(PlayerId.P1, Difficulty.Easy),
                AiPlayerFactory.Create(PlayerId.P2, Difficulty.Easy),
            };
            var session = new GameSession(BoardConfig.Kid, seats);
            int ply = 0;
            session.EventOccurred += e =>
            {
                switch (e)
                {
                    case PawnMoved: ply++; break;
                    case WallPlaced: ply++; break;
                    case PlayerWon pw:
                        Console.WriteLine($"  胜者: P{(int)pw.Who + 1}");
                        break;
                }
            };
            session.Start();

            bool ok = session.State.IsFinished && session.State.Winner is not null;
            Console.WriteLine($"  总手数: {ply}  终局: {(session.State.Winner is { } w ? $"P{(int)w + 1}胜" : "未结束")}");
            Console.WriteLine($"  记谱: {session.Export()}");

            if (!ok) { Console.WriteLine("  [FAIL] 未到达终局"); return 1; }
            Console.WriteLine("  [PASS]");
            return 0;
        }
        catch (Exception ex) { Console.WriteLine($"  [FAIL] 异常: {ex.Message}"); return 1; }
    }

    private static int Scenario2Preview()
    {
        Console.WriteLine("── 场景 2: 设墙预览合法/非法 ──");
        try
        {
            var state = GameSetup.CreateKid2P();

            // 合法: 角落墙, 不切断路径
            var legal = PreviewService.PoseWall(state, new WallPos(new Cell(0, 0), WallOrient.Horizontal));

            // 非法: 镜像 PreviewServiceTests 封死构造, Kid 角落同理
            var blocked = state with
            {
                Pawns = state.Pawns.Replace(state.PawnOf(PlayerId.P1), state.PawnOf(PlayerId.P1) with { Pos = new Cell(0, 0) }),
            };
            blocked = blocked with { Walls = blocked.Walls.Add(new WallPos(new Cell(0, 0), WallOrient.Vertical)) };
            var illegal = PreviewService.PoseWall(blocked, new WallPos(new Cell(0, 1), WallOrient.Horizontal));

            Console.WriteLine($"  合法墙: Legal={legal.Legal}  P1步={StepsOf(legal, PlayerId.P1)}  P2步={StepsOf(legal, PlayerId.P2)}");
            Console.WriteLine($"  非法墙: Legal={illegal.Legal}  Reason={illegal.Reason}");

            bool ok = legal.Legal && legal.Routes.Length == 2
                   && !illegal.Legal && illegal.Reason == RejectReason.WallBlocksAllPaths;
            if (!ok) { Console.WriteLine("  [FAIL] 预览断言不成立"); return 1; }
            Console.WriteLine("  [PASS]");
            return 0;
        }
        catch (Exception ex) { Console.WriteLine($"  [FAIL] 异常: {ex.Message}"); return 1; }
    }

    private static int StepsOf(PreviewResult r, PlayerId id)
    {
        foreach (var x in r.Routes) if (x.Pawn == id) return x.Steps;
        return -1;
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
