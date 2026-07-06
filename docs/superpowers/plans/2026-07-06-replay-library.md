# 回放预置棋局库 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 提供"观战 AI vs AI 棋局"能力:预置 18 局(9 难度组合 × 2 变体)完整自对弈记谱,玩家加载后用前进/后退/跳转/重置逐步观察。

**Architecture:** 复用既有 `ReplayController`(零改)+ `NotationService`。新增 `ReplayLibrary`(纯 C# 常量,18 条记谱)由生成器一次性产出。`GameViewRoot` 加 `Mode==Replay` 分支:不 `StartSession`,改用 `ReplayController`,HUD 加回放控制条。三层依赖不变。

**Tech Stack:** C# / .NET 10 / xUnit / Godot 4.7 mono。纯逻辑层(UI.Logic)TDD;Godot 脚本靠 `dotnet build` + 手动验收。

**Spec:** `docs/superpowers/specs/2026-07-06-replay-library-design.md`

---

## File Structure

| 文件 | 责任 | 动作 |
|---|---|---|
| `src/Quoridor.UI.Logic/ReplayLibrary.cs` | `ReplayEntry` record + `ReplayLibrary.All`(18 条常量) | 创建(骨架)→ 生成器覆盖数据 |
| `src/Quoridor.UI.Logic/GameConfig.cs` | `MatchMode` 加 `Replay` + `ReplayEntry? Replay` 字段 | 改 |
| `demo/Quoridor.Demo/Program.cs` | 加 `--gen-replays` 生成器 | 改 |
| `tests/Quoridor.UI.Logic.Tests/ReplayLibraryTests.cs` | 18 局可解码 + 回放到终局 + 胜者一致 | 创建 |
| `src/Quoridor.UI/Scripts/BoardView.cs` | `Init` 重载(接受初始 state,回放不依赖 Session) | 改 |
| `src/Quoridor.UI/Scripts/HudView.cs` | 回放控制按钮 + `ShowReplayMode`/`RefreshReplay` | 改 |
| `src/Quoridor.UI/Scripts/StartFrameView.cs` | 模式加"回放" + 棋局 OptionButton | 改 |
| `src/Quoridor.UI/Scripts/GameViewRoot.cs` | `Mode==Replay` 分支 + 回放按钮接线 | 改 |

---

## Task 1: ReplayEntry record + ReplayLibrary 骨架

**Files:**
- Create: `src/Quoridor.UI.Logic/ReplayLibrary.cs`

骨架先行,让 `GameConfig`(Task 2)能引用 `ReplayEntry` 类型编译。`All` 暂为空,Task 5 生成器覆盖为 18 条。

- [ ] **Step 1: 创建骨架文件**

`src/Quoridor.UI.Logic/ReplayLibrary.cs`:
```csharp
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;

namespace Quoridor.UI.Logic;

/// <summary>一条预置回放棋局: P1=先手方(P1Diff), P2=后手方(P2Diff)。Notation 为完整记谱串。</summary>
public sealed record ReplayEntry(
    string Name,
    BoardVariant Variant,
    Difficulty P1Diff,
    Difficulty P2Diff,
    PlayerId Winner,
    int Plies,
    string Notation);

/// <summary>预置 AI vs AI 棋局库(18 条 = 9 难度组合 × 2 变体)。由 demo --gen-replays 产出。</summary>
public static class ReplayLibrary
{
    public static IReadOnlyList<ReplayEntry> All { get; } = System.Array.Empty<ReplayEntry>();
}
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build src/Quoridor.UI.Logic/Quoridor.UI.Logic.csproj`
Expected: 成功,0 错误。

- [ ] **Step 3: Commit**

```bash
git add src/Quoridor.UI.Logic/ReplayLibrary.cs
git commit -m "feat(replay): ReplayEntry/ReplayLibrary 骨架(UI.Logic)"
```

---

## Task 2: GameConfig 扩展

**Files:**
- Modify: `src/Quoridor.UI.Logic/GameConfig.cs`
- Test: `tests/Quoridor.UI.Logic.Tests/ReplayLibraryTests.cs`(本任务仅加 GameConfig 构造测试,复用此文件)

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.UI.Logic.Tests/ReplayLibraryTests.cs`(新建):
```csharp
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Quoridor.UI.Logic;
using Xunit;

namespace Quoridor.UI.Logic.Tests;

public class ReplayLibraryTests
{
    [Fact]
    public void GameConfig_replay_mode_carries_entry()
    {
        var entry = new ReplayEntry("t", BoardVariant.Standard, Difficulty.Easy, Difficulty.Easy, PlayerId.P1, 0, "");
        var cfg = new GameConfig(BoardVariant.Standard, MatchMode.Replay, Difficulty.Easy, PlayerId.P1, entry);
        Assert.Equal(MatchMode.Replay, cfg.Mode);
        Assert.Same(entry, cfg.Replay);
    }
}
```

- [ ] **Step 2: 跑测试,确认失败**

Run: `dotnet test tests/Quoridor.UI.Logic.Tests --filter "FullyQualifiedName~GameConfig_replay"`
Expected: 编译失败——`MatchMode.Replay` 不存在、`GameConfig` 无 `Replay` 字段。

- [ ] **Step 3: 改 GameConfig**

`src/Quoridor.UI.Logic/GameConfig.cs` 改为:
```csharp
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;

namespace Quoridor.UI.Logic;

public enum MatchMode { VsAi, HotSeat, Replay }

/// <summary>StartFrame → MainController → GameView 的开局契约。
/// Replay 模式: Replay 非 null, Variant 取自 Replay.Variant, AiDifficulty/FirstMove 忽略。</summary>
public sealed record GameConfig(
    BoardVariant Variant,
    MatchMode Mode,
    Difficulty AiDifficulty,
    PlayerId FirstMove,
    ReplayEntry? Replay = null);
```

- [ ] **Step 4: 跑测试,确认通过**

Run: `dotnet test tests/Quoridor.UI.Logic.Tests --filter "FullyQualifiedName~GameConfig_replay"`
Expected: PASS。

- [ ] **Step 5: 全量回归**

Run: `dotnet test Quoridor.slnx`
Expected: 139 全绿(新增 1,共 140)。

- [ ] **Step 6: Commit**

```bash
git add src/Quoridor.UI.Logic/GameConfig.cs tests/Quoridor.UI.Logic.Tests/ReplayLibraryTests.cs
git commit -m "feat(replay): GameConfig 加 MatchMode.Replay + Replay 字段"
```

---

## Task 3: 生成器(demo `--gen-replays`)

**Files:**
- Modify: `demo/Quoridor.Demo/Program.cs`

生成器遍历 9 难度组合 × 2 变体,每局 `GameSession(autoDriveAi:true)` 同步自对弈到终局,`Export()` 取记谱,产出 `ReplayLibrary.cs`(含 record 定义 + 18 条数据)。

- [ ] **Step 1: 加生成器代码**

在 `demo/Quoridor.Demo/Program.cs` 的 `Main` 开头(args 解析处)加分支:
```csharp
if (args.Length > 0 && args[0].Equals("--gen-replays", StringComparison.OrdinalIgnoreCase))
{
    GenReplays();
    return;
}
```

在 `Program` 类内加方法:
```csharp
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
    while (dir != null && !dir.GetDirectories(".git").Any()) dir = dir.Parent;
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
```

需在 `Program.cs` 顶部加 `using Quoridor.Domain.Core;`(若未有)。`PawnMoved`/`WallPlaced` 在 `Quoridor.Domain.Core`(已 using)。`AiPlayerFactory` 在 `Quoridor.Application.Seats`(demo 已 using)。

- [ ] **Step 2: 编译验证**

Run: `dotnet build demo/Quoridor.Demo/Quoridor.Demo.csproj`
Expected: 0 错误。

- [ ] **Step 3: 冒烟(只跑 Easy-Easy Kid,验证产物格式)**

临时手测生成器产出格式,不跑全量(全量在 Task 5)。可跳过,直接 Task 5 验证。若想先验格式:
Run: `dotnet run --project demo/Quoridor.Demo -- --gen-replays`(会跑全量,~1h;或先信任代码,Task 5 跑)

- [ ] **Step 4: Commit**

```bash
git add demo/Quoridor.Demo/Program.cs
git commit -m "feat(replay): demo --gen-replays 生成器(产出 ReplayLibrary.cs)"
```

---

## Task 4: ReplayLibraryTests(红)

**Files:**
- Modify: `tests/Quoridor.UI.Logic.Tests/ReplayLibraryTests.cs`

写完整测试(Count==18 等)。此时 `All` 仍空(Task 1 骨架),测试红。

- [ ] **Step 1: 追加测试到 ReplayLibraryTests.cs**

在类内追加(Task 2 的 GameConfig 测试保留):
```csharp
[Fact]
public void All_has_18_entries() => Assert.Equal(18, ReplayLibrary.All.Count);

[Fact]
public void Covers_9_diff_pairs_times_2_variants()
{
    var std = ReplayLibrary.All.Where(e => e.Variant == BoardVariant.Standard).ToList();
    var kid = ReplayLibrary.All.Where(e => e.Variant == BoardVariant.Kid).ToList();
    Assert.Equal(9, std.Count);
    Assert.Equal(9, kid.Count);
    var keys = std.Select(e => (e.P1Diff, e.P2Diff)).ToHashSet();
    Assert.Equal(9, keys.Count);
}

[Theory]
[InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)]
[InlineData(5)][InlineData(6)][InlineData(7)][InlineData(8)][InlineData(9)]
[InlineData(10)][InlineData(11)][InlineData(12)][InlineData(13)][InlineData(14)]
[InlineData(15)][InlineData(16)][InlineData(17)]
public void Each_replay_decodes_and_plays_to_stated_winner(int i)
{
    var e = ReplayLibrary.All[i];
    var cfg = e.Variant == BoardVariant.Kid ? BoardConfig.Kid : BoardConfig.Standard;
    var rc = new Quoridor.Application.ReplayController(cfg, 2, e.Notation);
    rc.GoTo(rc.Total);
    Assert.True(rc.Current.IsFinished);
    Assert.Equal(e.Winner, rc.Current.Winner);
    Assert.Equal(e.Plies, rc.Total);
}
```

需在测试文件顶部加 `using Quoridor.Application;`(ReplayController)。

- [ ] **Step 2: 跑测试,确认红**

Run: `dotnet test tests/Quoridor.UI.Logic.Tests --filter "FullyQualifiedName~ReplayLibraryTests"`
Expected: `All_has_18_entries` FAIL(实际 0,期望 18);`Each_replay...` 越界或空。

- [ ] **Step 3: Commit(红测试)**

```bash
git add tests/Quoridor.UI.Logic.Tests/ReplayLibraryTests.cs
git commit -m "test(replay): ReplayLibrary 18 局测试(红, 待生成器产出数据)"
```

---

## Task 5: 跑生成器产出 18 条(绿)

**Files:**
- Overwrite: `src/Quoridor.UI.Logic/ReplayLibrary.cs`(由生成器产出)

- [ ] **Step 1: 运行生成器(后台,~1h)**

Run(后台,Hard 局慢):
```bash
dotnet run --project demo/Quoridor.Demo -- --gen-replays
```
Expected stdout: 18 行 "生成 {Variant} {P1} vs {P2} ... N手 胜=PX",末尾 "已写 18 条到 ReplayLibrary.cs"。

- [ ] **Step 2: 验证产物**

Run: `grep -c "new ReplayEntry" src/Quoridor.UI.Logic/ReplayLibrary.cs`
Expected: 18

- [ ] **Step 3: 跑 ReplayLibraryTests,确认绿**

Run: `dotnet test tests/Quoridor.UI.Logic.Tests --filter "FullyQualifiedName~ReplayLibraryTests"`
Expected: 全 PASS(Count==18 + 18 局回放到终局 + 胜者一致)。

- [ ] **Step 4: 全量回归**

Run: `dotnet test Quoridor.slnx`
Expected: 全绿(140 + ReplayLibrary 新增 20 = ~160)。

- [ ] **Step 5: Commit**

```bash
git add src/Quoridor.UI.Logic/ReplayLibrary.cs
git commit -m "feat(replay): 预置 18 局 AI vs AI 棋局(生成器产出)"
```

---

## Task 6: BoardView.Init 重载

**Files:**
- Modify: `src/Quoridor.UI/Scripts/BoardView.cs`

回放模式无 Session,`Init` 需接受外部初始 state 建棋子。

- [ ] **Step 1: 加重载**

在 `BoardView.cs` 的 `Init(MainController ctrl)` 旁加重载,原方法委托:
```csharp
public void Init(MainController ctrl) => Init(ctrl, ctrl.Session!.State);

public void Init(MainController ctrl, GameState initial)
{
    _ctrl = ctrl;
    var board = ctrl.BoardConfig;
    _layout = new BoardLayout(board, 1.0f);
    BuildBoard(board);
    BuildPawns(initial);
}
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build src/Quoridor.UI/Quoridor.UI.csproj`
Expected: 0 错误。

- [ ] **Step 3: Commit**

```bash
git add src/Quoridor.UI/Scripts/BoardView.cs
git commit -m "feat(replay): BoardView.Init 重载(接受初始 state, 回放不依赖 Session)"
```

---

## Task 7: HudView 回放控制

**Files:**
- Modify: `src/Quoridor.UI/Scripts/HudView.cs`

加回放控制按钮(⏮⬅➡⏭ + 步数)+ `ShowReplayMode`/`RefreshReplay`。

- [ ] **Step 1: 加字段与按钮**

在 `HudView` 字段区加:
```csharp
private Button _reset = new() { Text = "⏮" };
private Button _back = new() { Text = "⬅" };
private Button _fwd = new() { Text = "➡" };
private Button _toEnd = new() { Text = "⏭" };
private Label _stepLabel = new();

public event Action? ReplayResetRequested;
public event Action? ReplayBackRequested;
public event Action? ReplayForwardRequested;
public event Action? ReplayToEndRequested;
```

- [ ] **Step 2: 在 `Init` 末尾(`AddChild` 行)前配置回放按钮**

在 `Init` 方法内、`AddChild(_top)...` 行之前加:
```csharp
_reset.MouseFilter = Control.MouseFilterEnum.Stop;
_back.MouseFilter = Control.MouseFilterEnum.Stop;
_fwd.MouseFilter = Control.MouseFilterEnum.Stop;
_toEnd.MouseFilter = Control.MouseFilterEnum.Stop;
_stepLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
int bx = 10;
foreach (var b in new[] { _reset, _back, _fwd, _toEnd })
{ b.Position = new Vector2(bx, 690); b.Size = new Vector2(50, 40); bx += 60; }
_stepLabel.Position = new Vector2(bx + 10, 690); _stepLabel.Size = new Vector2(200, 40);
_stepLabel.AddThemeFontSizeOverride("font_size", 20);
var replayBtns = new Control[] { _reset, _back, _fwd, _toEnd, _stepLabel };
foreach (var b in replayBtns) b.Visible = false;
_reset.Pressed += () => ReplayResetRequested?.Invoke();
_back.Pressed += () => ReplayBackRequested?.Invoke();
_fwd.Pressed += () => ReplayForwardRequested?.Invoke();
_toEnd.Pressed += () => ReplayToEndRequested?.Invoke();
```

把 `AddChild` 行改为也加入回放按钮:
```csharp
AddChild(_top); AddChild(_status); AddChild(_notation); AddChild(_budget);
AddChild(_footer); AddChild(_home); AddChild(_surrender);
AddChild(_reset); AddChild(_back); AddChild(_fwd); AddChild(_toEnd); AddChild(_stepLabel);
```

- [ ] **Step 3: 加 ShowReplayMode / RefreshReplay 方法**

在 `HudView` 内加:
```csharp
/// <summary>切换回放模式: 显示回放控制按钮, 隐藏投降/AI思考相关。</summary>
public void ShowReplayMode(bool on)
{
    _reset.Visible = on; _back.Visible = on; _fwd.Visible = on; _toEnd.Visible = on; _stepLabel.Visible = on;
    _surrender.Visible = !on;
}

/// <summary>刷新回放状态: 当前手/总手 + 当前轮次 + 棋局名。</summary>
public void RefreshReplay(ReplayEntry entry, Quoridor.Application.ReplayController replay)
{
    int n = _seatMap.ToDisplayNumber(replay.Current.ActivePlayer);
    if (replay.Current.IsFinished && replay.Current.Winner is { } w)
    {
        int wn = _seatMap.ToDisplayNumber(w);
        _status.Text = $"★ 玩家{wn} 获胜! ★";
        _status.AddThemeColorOverride("font_color", wn == 1 ? P1Color : P2Color);
    }
    else
    {
        _status.Text = $"{entry.Name} · 轮到 玩家{n}";
        _status.AddThemeColorOverride("font_color", n == 1 ? P1Color : P2Color);
    }
    _stepLabel.Text = $"{replay.Cursor} / {replay.Total}";
    _budget.Text = entry.Name;
}
```

- [ ] **Step 4: 编译验证**

Run: `dotnet build src/Quoridor.UI/Quoridor.UI.csproj`
Expected: 0 错误。

- [ ] **Step 5: Commit**

```bash
git add src/Quoridor.UI/Scripts/HudView.cs
git commit -m "feat(replay): HUD 回放控制条(⏮⬅➡⏭ + 步数) + ShowReplayMode/RefreshReplay"
```

---

## Task 8: StartFrameView 回放模式

**Files:**
- Modify: `src/Quoridor.UI/Scripts/StartFrameView.cs`

模式加"回放",选中后显示棋局 OptionButton(18 项),隐藏难度/先手。

- [ ] **Step 1: 加棋局 OptionButton 字段**

在 `StartFrameView` 字段区加:
```csharp
private OptionButton _replay = new();
```

- [ ] **Step 2: 在 `_Ready` 配置回放 OptionButton**

在 `_Ready` 的 `AddItem` 区块后加:
```csharp
foreach (var e in ReplayLibrary.All)
    _replay.AddItem($"{e.Name} · P{(int)e.Winner + 1}胜 · {e.Plies}手");
_replay.Selected = 0;
_replay.Visible = false;
```

把 `_replay` 加入 `_Ready` 末尾的控件循环(让它也被 AddChild + 定位)。改循环:
```csharp
int y = 20;
foreach (var c in new Control[] { _variant, _mode, _diff, _first, _replay, _start })
{ c.Position = new Vector2(40, y); c.Size = new Vector2(360, 30); AddChild(c); y += 50; }
```

- [ ] **Step 3: 改模式切换逻辑**

替换原 `_mode.ItemSelected` 行为:
```csharp
_mode.ItemSelected += idx =>
{
    bool vsAi = idx == 0;
    bool hotSeat = idx == 1;
    bool replay = idx == 2;
    _diff.Visible = vsAi || replay;
    _first.Visible = vsAi || hotSeat;
    _variant.Visible = !replay;       // 回放时变体由棋局决定
    _replay.Visible = replay;
};
```

并在 `_Ready` 加 `_mode.AddItem("回放 AI vs AI", 2);`(在现有 `AddItem` 之后)。

- [ ] **Step 4: 改 `OnStart` 处理回放**

`OnStart` 顶部加:
```csharp
if (_mode.Selected == 2)
{
    var entry = ReplayLibrary.All[_replay.Selected];
    var cfg = new GameConfig(entry.Variant, MatchMode.Replay, Difficulty.Easy, PlayerId.P1, entry);
    ctrl.Config = cfg;
    GD.Print($"StartFrame: replay={entry.Name}");
    GetTree().ChangeSceneToFile("res://Scenes/GameView.tscn");
    return;
}
```

需 `using Quoridor.UI.Logic;`(已有)。

- [ ] **Step 5: 编译验证**

Run: `dotnet build src/Quoridor.UI/Quoridor.UI.csproj`
Expected: 0 错误。

- [ ] **Step 6: Commit**

```bash
git add src/Quoridor.UI/Scripts/StartFrameView.cs
git commit -m "feat(replay): StartFrame 加回放模式 + 棋局 OptionButton"
```

---

## Task 9: GameViewRoot 回放分支

**Files:**
- Modify: `src/Quoridor.UI/Scripts/GameViewRoot.cs`

`Mode==Replay` → 不 `StartSession`,用 `ReplayController`,接回放按钮。

- [ ] **Step 1: 加 ReplayController 字段与 using**

在 `GameViewRoot.cs` 顶部 `using` 区确保有 `using Quoridor.Application;`(已有)。加字段:
```csharp
private ReplayController? _replay;
```

- [ ] **Step 2: 改 `_Ready` 加回放分支**

把现有 `_Ready` 中 `_ctrl.StartSession(cfg)` 起的部分改为分模式:
```csharp
var cfg = _ctrl.Config!;
if (cfg.Mode == MatchMode.Replay && cfg.Replay is { } entry)
{
    BuildScene();
    var board = entry.Variant == BoardVariant.Kid ? BoardConfig.Kid : BoardConfig.Standard;
    _replay = new ReplayController(board, 2, entry.Notation);
    _board!.Init(_ctrl, _replay.Current);
    _preview!.Init(_board.Layout);
    _hud!.Init(SeatMap.ForFirstMove(PlayerId.P1));
    _hud.ShowReplayMode(true);

    _hud.ReplayResetRequested += () => OnReplayStep(ReplayAction.Reset);
    _hud.ReplayBackRequested += () => OnReplayStep(ReplayAction.Back);
    _hud.ReplayForwardRequested += () => OnReplayStep(ReplayAction.Forward);
    _hud.ReplayToEndRequested += () => OnReplayStep(ReplayAction.ToEnd);

    _board.Render(_replay.Current);
    _hud.RefreshReplay(entry, _replay);
    return;
}

// 现有路径(VsAi/HotSeat)不变:
_ctrl.StartSession(cfg);
_board!.Init(_ctrl);
_preview!.Init(_board.Layout);
_hud!.Init(SeatMap.ForFirstMove(cfg.FirstMove));
_hud.ShowReplayMode(false);
// ... 余下事件订阅 + Start + KickAi 保持原样
```

注意:原 `_Ready` 在 `StartSession` 后的事件订阅(`CellClicked` 等)只在非回放分支需要。回放分支不订阅 `CellClicked`(棋盘不可点)。把 `CellClicked`/`SlotHovered`/`SlotClicked`/`SlotCleared`/`BackToStartRequested`/`SurrenderRequested`/`EventOccurred` 订阅留在非回放分支。`BackToStartRequested` 两分支都要(回放也要能回开始页)——在回放分支也加 `_hud.BackToStartRequested += OnBackToStart;`。

为最小改动,建议把"两分支共用"的订阅(`BackToStartRequested`)放分支前,其余放非回放分支。重组 `_Ready`:
```csharp
public override void _Ready()
{
    _ctrl = GetNode<MainController>("/root/MainController");
    BuildScene();
    var cfg = _ctrl.Config!;

    _board!.Init(_ctrl);            // 对局分支用 Session.State; 回放分支下面重新 Init 覆盖
    _preview!.Init(_board.Layout);
    _hud!.Init(SeatMap.ForFirstMove(cfg.Mode == MatchMode.Replay ? PlayerId.P1 : cfg.FirstMove));
    _hud.BackToStartRequested += OnBackToStart;

    if (cfg.Mode == MatchMode.Replay && cfg.Replay is { } entry)
    {
        var board = entry.Variant == BoardVariant.Kid ? BoardConfig.Kid : BoardConfig.Standard;
        _replay = new ReplayController(board, 2, entry.Notation);
        _board.Init(_ctrl, _replay.Current);   // 覆盖: 用回放初始 state
        _hud.ShowReplayMode(true);
        _hud.ReplayResetRequested += () => OnReplayStep(ReplayAction.Reset);
        _hud.ReplayBackRequested += () => OnReplayStep(ReplayAction.Back);
        _hud.ReplayForwardRequested += () => OnReplayStep(ReplayAction.Forward);
        _hud.ReplayToEndRequested += () => OnReplayStep(ReplayAction.ToEnd);
        _board.Render(_replay.Current);
        _hud.RefreshReplay(entry, _replay);
        return;
    }

    // 对局分支
    _hud.ShowReplayMode(false);
    _ctrl.StartSession(cfg);
    _board.Init(_ctrl, _ctrl.Session!.State);   // 用 Session state 覆盖
    _board.CellClicked += OnCellClicked;
    _board.SlotHovered += OnSlotHovered;
    _board.SlotClicked += OnSlotClicked;
    _board.SlotCleared += () => _preview.Clear();
    _hud.SurrenderRequested += OnSurrender;
    _ctrl.Session!.EventOccurred += OnEvent;
    _hud.RefreshTop(_ctrl.Session.State, cfg);
    _board.Render(_ctrl.Session.State);
    _ctrl.Session.Start();
    KickAiIfNeeded();
}
```

注意:原代码 `_board.Init(_ctrl)` 只调一次。新代码先 `Init(_ctrl)`(建布局+棋盘+棋子用 Session.State,但回放分支 Session 为 null!)。问题:回放分支 `_ctrl.Session` null,`Init(_ctrl)` 内 `ctrl.Session!.State` 崩。

解法:回放分支**不**先调 `Init(_ctrl)`。重组:只在确定分支后调对应 Init。即把 `_board.Init` 移到分支内。重写:
```csharp
public override void _Ready()
{
    _ctrl = GetNode<MainController>("/root/MainController");
    BuildScene();
    var cfg = _ctrl.Config!;

    _preview!.Init(_board!.Layout);   // 注意: _board 在 BuildScene 已 new, Layout 在 Init 后才有
    // ↑ _preview.Init 需要 _board.Layout, 而 Layout 在 _board.Init 后才赋值。故 _preview.Init 必须在 _board.Init 后。
    ...
}
```

`_preview.Init(_board.Layout)` 依赖 `_board.Init` 先跑(`Layout` 在 `Init` 内赋值)。所以顺序必须 `_board.Init` → `_preview.Init`。两分支都要。

最终 `_Ready`(两分支各自 Init board 再 Init preview):
```csharp
public override void _Ready()
{
    _ctrl = GetNode<MainController>("/root/MainController");
    BuildScene();
    var cfg = _ctrl.Config!;

    _hud!.Init(SeatMap.ForFirstMove(cfg.Mode == MatchMode.Replay ? PlayerId.P1 : cfg.FirstMove));
    _hud.BackToStartRequested += OnBackToStart;

    if (cfg.Mode == MatchMode.Replay && cfg.Replay is { } entry)
    {
        var board = entry.Variant == BoardVariant.Kid ? BoardConfig.Kid : BoardConfig.Standard;
        _replay = new ReplayController(board, 2, entry.Notation);
        _board!.Init(_ctrl, _replay.Current);
        _preview!.Init(_board.Layout);
        _hud.ShowReplayMode(true);
        _hud.ReplayResetRequested += () => OnReplayStep(ReplayAction.Reset);
        _hud.ReplayBackRequested += () => OnReplayStep(ReplayAction.Back);
        _hud.ReplayForwardRequested += () => OnReplayStep(ReplayAction.Forward);
        _hud.ReplayToEndRequested += () => OnReplayStep(ReplayAction.ToEnd);
        _board.Render(_replay.Current);
        _hud.RefreshReplay(entry, _replay);
        return;
    }

    _ctrl.StartSession(cfg);
    _board!.Init(_ctrl, _ctrl.Session!.State);
    _preview!.Init(_board.Layout);
    _hud.ShowReplayMode(false);
    _board.CellClicked += OnCellClicked;
    _board.SlotHovered += OnSlotHovered;
    _board.SlotClicked += OnSlotClicked;
    _board.SlotCleared += () => _preview.Clear();
    _hud.SurrenderRequested += OnSurrender;
    _ctrl.Session!.EventOccurred += OnEvent;
    _hud.RefreshTop(_ctrl.Session.State, cfg);
    _board.Render(_ctrl.Session.State);
    _ctrl.Session.Start();
    KickAiIfNeeded();
}
```

- [ ] **Step 3: 加 ReplayAction 枚举与 OnReplayStep 方法**

在 `GameViewRoot` 内加:
```csharp
private enum ReplayAction { Reset, Back, Forward, ToEnd }

private void OnReplayStep(ReplayAction a)
{
    if (_replay is null || _ctrl?.Config?.Replay is not { } entry) return;
    switch (a)
    {
        case ReplayAction.Reset: _replay.Reset(); break;
        case ReplayAction.Back: _replay.StepBack(); break;
        case ReplayAction.Forward: _replay.StepForward(); break;
        case ReplayAction.ToEnd: _replay.GoTo(_replay.Total); break;
    }
    _board!.Render(_replay.Current);
    _hud!.RefreshReplay(entry, _replay);
}
```

- [ ] **Step 4: 改 `_ExitTree` 解订阅(回放分支无 Session 事件)**

`_ExitTree` 现有解订阅 `Session.EventOccurred`,回放模式 Session 为 null。改为:
```csharp
public override void _ExitTree()
{
    if (_ctrl?.Session is { } s) s.EventOccurred -= OnEvent;
    _ctrl?.EndSession();
}
```
回放模式 `_ctrl.Session` null,跳过解订阅,安全。

- [ ] **Step 5: 编译验证**

Run: `dotnet build src/Quoridor.UI/Quoridor.UI.csproj`
Expected: 0 错误。

- [ ] **Step 6: Commit**

```bash
git add src/Quoridor.UI/Scripts/GameViewRoot.cs
git commit -m "feat(replay): GameViewRoot 回放分支(ReplayController + 控制条接线)"
```

---

## Task 10: 全量回归 + 构建 + 手动验收

- [ ] **Step 1: 全量测试**

Run: `dotnet test Quoridor.slnx`
Expected: 全绿(Domain 74 + UI.Logic 24+20 + Application 41 ≈ 159)。

- [ ] **Step 2: 构建 Godot 项目**

Run: `dotnet build src/Quoridor.UI/Quoridor.UI.csproj`
Expected: 0 错误(1 个 pre-existing CS8604 warning 可忽略)。

- [ ] **Step 3: 启动 GUI 手动验收**

Run: `cd src/Quoridor.UI && godot-mono --path .`

验收清单:
1. StartFrame 模式选"回放" → 难度/先手/变体隐藏,显示棋局 OptionButton(18 项)。
2. 选一局(如 "Standard · Easy vs Easy")→ 开始 → 进 GameView。
3. 大字状态行显示棋局名 + 当前轮次;底部 ⏮⬅➡⏭ + 步数 "0 / N"。
4. 点 ➡ 多次:棋子/墙逐步推进,步数递增,记谱区滚动。
5. 点 ⬅:回退一步。
6. 点 ⏭:跳到终局,大字"★ 玩家X 获胜! ★"。
7. 点 ⏮:回到开局。
8. 点"回到开始页"返回。
9. 选 Hard 局(如 "Standard · Hard vs Hard")→ 加载正常(预置已演算,无卡顿)。
10. VsAi/HotSeat 模式仍正常(回归)。

- [ ] **Step 4: 提交收尾**

```bash
git add -A
git commit -m "feat(replay): 回放预置棋局库完成(18 局 + 回放 UI)"
```

---

## Self-Review(计划自审)

- **Spec 覆盖**:ReplayEntry/ReplayLibrary(Task 1,5)✅;GameConfig 扩展(Task 2)✅;生成器(Task 3,5)✅;StartFrame 回放入口(Task 8)✅;GameViewRoot 回放分支(Task 9)✅;HUD 控制条(Task 7)✅;BoardView.Init 重载(Task 6)✅;测试 18 局(Task 4,5)✅;错误处理(记谱合法由生成器保证 + 测试锁定)✅;非目标(运行时生成/4人)未触 ✅。
- **Placeholder**:无 TBD/TODO;所有代码步骤含完整代码。
- **类型一致**:`ReplayEntry` 字段(Name/Variant/P1Diff/P2Diff/Winner/Plies/Notation)在 Task 1、生成器 Task 3、HUD Task 7、GameViewRoot Task 9 一致。`MatchMode.Replay` 一致。`ShowReplayMode`/`RefreshReplay` 一致。`ReplayAction` 枚举一致。
- **顺序依赖**:Task 1(类型)→ Task 2(GameConfig 引用 ReplayEntry)→ Task 3(生成器)→ Task 4(测试红)→ Task 5(数据绿)→ Task 6-9(UI)→ Task 10。Task 5 依赖 Task 3 生成器代码。✅
