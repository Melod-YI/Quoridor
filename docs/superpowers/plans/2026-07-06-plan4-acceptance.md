# Plan 4 验收项 2-7 收口 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把 Plan 4 验收项 2-7 真正收口——补 4 条 PreviewService 测试、加 demo `--acceptance` 半自动回归、写 7 项手动验收清单。

**Architecture:** 三层独立交付,不加生产代码。(a) xUnit 测试补强 `PreviewServiceTests.cs`;(b) `demo/Quoridor.Demo` 加 `--acceptance` 子命令跑换位断言 + Kid 终局 + 预览合法非法;(c) 手动验收清单文档。spec 见 `docs/superpowers/specs/2026-07-06-plan4-acceptance-design.md`。

**Tech Stack:** .NET 10 / xUnit / C# / Godot 4.7 mono(仅 headless 加载检查)。

---

## 文件结构

| 文件 | 责任 | 动作 |
|------|------|------|
| `tests/Quoridor.Application.Tests/PreviewServiceTests.cs` | PreviewService 单测 | 末尾追加 4 条测试 + 2 个 using |
| `demo/Quoridor.Demo/Quoridor.Demo.csproj` | demo 项目 | 加 UI.Logic ProjectReference |
| `demo/Quoridor.Demo/Program.cs` | demo 入口 | Main 改 `int`、加 `--acceptance` 分支 + 3 场景方法 + helper |
| `docs/superpowers/acceptance/2026-07-06-plan4-acceptance-checklist.md` | 手动验收清单 | 新建 |

---

## Task 1: PreviewService Kid 合法墙测试

**Files:**
- Modify: `tests/Quoridor.Application.Tests/PreviewServiceTests.cs`(末尾追加,`class PreviewServiceTests` 内)

- [ ] **Step 1: 写失败测试**

在 `PreviewServiceTests.cs` 的 `private static int StepsOf(...)` 方法之前追加:

```csharp
[Fact]
public void Legal_wall_on_Kid_returns_routes()
{
    var state = GameSetup.CreateKid2P();
    var wall = new WallPos(new Cell(0, 0), WallOrient.Horizontal);  // Kid 7×7 角落, 不切断路径

    var r = PreviewService.PoseWall(state, wall);

    Assert.True(r.Legal);
    Assert.Null(r.Reason);
    Assert.Equal(2, r.Routes.Length);
    Assert.Contains(r.Routes, x => x.Pawn == PlayerId.P1 && x.Steps >= 0);
    Assert.Contains(r.Routes, x => x.Pawn == PlayerId.P2 && x.Steps >= 0);
}
```

- [ ] **Step 2: 跑测试**

Run: `dotnet test tests/Quoridor.Application.Tests/Quoridor.Application.Tests.csproj --filter "FullyQualifiedName~Legal_wall_on_Kid_returns_routes"`
Expected: PASS(PreviewService 已实现,Kid 变体下 BoardGraph/PathFinder 行为与 Standard 一致)。

> 若 FAIL,先核对 wall 是否合法(角落 H(0,0) 不应切断任何方路径)——不应改生产代码。

- [ ] **Step 3: Commit**

```bash
git add tests/Quoridor.Application.Tests/PreviewServiceTests.cs
git commit -m "test(preview): Kid 变体合法墙返回双路线"
```

---

## Task 2: PreviewService Kid 封死墙非法测试

**Files:**
- Modify: `tests/Quoridor.Application.Tests/PreviewServiceTests.cs`

- [ ] **Step 1: 写测试**

在 Task 1 的测试后追加:

```csharp
[Fact]
public void Wall_blocking_paths_on_Kid_is_illegal()
{
    // 镜像 Standard 版 Wall_blocking_all_paths_is_illegal 的构造, Kid 7×7 角落同理封死。
    // P1 移到 (0,0); V(0,0) 阻东[(0,0)-(1,0) 与 (0,1)-(1,1)]; 预览 H(0,1) 阻北[(0,1)-(0,2)]。
    // P1 可达集 = {(0,0),(0,1)}, 均不在北边 row=6 → WallBlocksAllPaths。
    var s = GameSetup.CreateKid2P();
    s = s with
    {
        Pawns = s.Pawns.Replace(s.PawnOf(PlayerId.P1), s.PawnOf(PlayerId.P1) with { Pos = new Cell(0, 0) }),
    };
    s = s with { Walls = s.Walls.Add(new WallPos(new Cell(0, 0), WallOrient.Vertical)) };

    var r = PreviewService.PoseWall(s, new WallPos(new Cell(0, 1), WallOrient.Horizontal));

    Assert.False(r.Legal);
    Assert.Equal(RejectReason.WallBlocksAllPaths, r.Reason);
}
```

- [ ] **Step 2: 跑测试**

Run: `dotnet test tests/Quoridor.Application.Tests/Quoridor.Application.Tests.csproj --filter "FullyQualifiedName~Wall_blocking_paths_on_Kid_is_illegal"`
Expected: PASS。

- [ ] **Step 3: Commit**

```bash
git add tests/Quoridor.Application.Tests/PreviewServiceTests.cs
git commit -m "test(preview): Kid 变体封死路径墙非法"
```

---

## Task 3: PreviewService 精确步数测试

**Files:**
- Modify: `tests/Quoridor.Application.Tests/PreviewServiceTests.cs`

- [ ] **Step 1: 写测试(带可调整的期望值)**

在 Task 2 后追加。Kid 初始局面无墙:P1(3,0)→北 row6 = 6 步;P2(3,6)→南 row0 = 6 步。预览墙 H(0,0) 在角落,不影响列 3 南北通路,步数应不变。

```csharp
[Fact]
public void Step_count_is_exact_for_known_position()
{
    var state = GameSetup.CreateKid2P();  // P1(3,0)→北 row6, P2(3,6)→南 row0, 无墙
    var wall = new WallPos(new Cell(0, 0), WallOrient.Horizontal);  // 角落, 不影响列 3

    var r = PreviewService.PoseWall(state, wall);

    // 无墙直线距离: P1 row0→row6 = 6 步, P2 row6→row0 = 6 步。
    Assert.Equal(6, StepsOf(r, PlayerId.P1));
    Assert.Equal(6, StepsOf(r, PlayerId.P2));
}
```

- [ ] **Step 2: 跑测试,确认期望值**

Run: `dotnet test tests/Quoridor.Application.Tests/Quoridor.Application.Tests.csproj --filter "FullyQualifiedName~Step_count_is_exact_for_known_position" --logger "console;verbosity=detailed"`

若 PASS:步数确为 6,继续 Step 3。
若 FAIL(实际值非 6):把上面两个 `Assert.Equal(6, ...)` 的 `6` 改成失败信息里显示的实际值,**不改生产代码**。这是 characterization 测试,锁定 PathFinder 当前行为。改后重跑至 PASS。

- [ ] **Step 3: Commit**

```bash
git add tests/Quoridor.Application.Tests/PreviewServiceTests.cs
git commit -m "test(preview): 锁定已知局面精确步数"
```

---

## Task 4: PreviewService 忽略墙数(文档化设计决策)

**Files:**
- Modify: `tests/Quoridor.Application.Tests/PreviewServiceTests.cs`(顶部 using 区 + 末尾测试)

- [ ] **Step 1: 加 using**

在 `PreviewServiceTests.cs` 顶部 using 区(`using Xunit;` 之前)追加两行:

```csharp
using System.Collections.Immutable;
using System.Linq;
```

- [ ] **Step 2: 写测试**

在 Task 3 测试后追加:

```csharp
[Fact]
public void Preview_ignores_wall_budget()
{
    // 设计决策文档化: 预览只管结构合法性(重叠/越界/可达性), 不管墙数。
    // 墙耗尽禁拾由 BoardView.Render(wallable = WallsLeft>0 && !IsFinished) 另管, 与预览解耦。
    var s = GameSetup.CreateKid2P();
    s = s with
    {
        Players = s.Players.Select(p => p with { WallsLeft = 0 }).ToImmutableArray(),
    };

    var r = PreviewService.PoseWall(s, new WallPos(new Cell(0, 0), WallOrient.Horizontal));

    Assert.True(r.Legal);
    Assert.Null(r.Reason);
}
```

- [ ] **Step 3: 跑测试**

Run: `dotnet test tests/Quoridor.Application.Tests/Quoridor.Application.Tests.csproj --filter "FullyQualifiedName~Preview_ignores_wall_budget"`
Expected: PASS(PreviewService 用 `WallLegality.Validate`,不含墙数检查)。

- [ ] **Step 4: 跑整个 PreviewService 测试套确认无回归**

Run: `dotnet test tests/Quoridor.Application.Tests/Quoridor.Application.Tests.csproj --filter "FullyQualifiedName~PreviewServiceTests"`
Expected: 全 PASS(原 5 条 + 新 4 条 = 9 条)。

- [ ] **Step 5: Commit**

```bash
git add tests/Quoridor.Application.Tests/PreviewServiceTests.cs
git commit -m "test(preview): 文档化预览忽略墙数(与 BoardView 禁拾解耦)"
```

---

## Task 5: demo 加 UI.Logic 引用 + Main 改 int + --acceptance 分支骨架

**Files:**
- Modify: `demo/Quoridor.Demo/Quoridor.Demo.csproj`
- Modify: `demo/Quoridor.Demo/Program.cs`

- [ ] **Step 1: 加 ProjectReference**

在 `Quoridor.Demo.csproj` 的 `<ItemGroup>` 里(现有两个 ProjectReference 后)追加:

```xml
    <ProjectReference Include="..\..\src\Quoridor.UI.Logic\Quoridor.UI.Logic.csproj" />
```

- [ ] **Step 2: Main 改返回 int + 加 --acceptance 分支**

`Program.cs` 顶部 using 区追加(`using Quoridor.Domain.Notation;` 后):

```csharp
using Quoridor.Domain.Rules;
using Quoridor.UI.Logic;
```

把 `private static void Main(string[] args)` 改成 `private static int Main(string[] args)`,在 `--gen-replays` 分支后追加 `--acceptance` 分支。改后的 Main 开头:

```csharp
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
```

Main 现有末尾(`Console.WriteLine($"记谱: {session.Export()}");` 之后)追加 `return 0;`:

```csharp
    Console.WriteLine($"记谱: {session.Export()}");
    return 0;
}
```

- [ ] **Step 3: 加 RunAcceptance 骨架**

在 `Main` 方法后追加(场景方法在后续 Task 实现,这里先放占位返回 0 保证编译):

```csharp
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

private static int Scenario1Swap() => 0;
private static int Scenario1KidGame() => 0;
private static int Scenario2Preview() => 0;
```

- [ ] **Step 4: 构建 demo 确认编译**

Run: `dotnet build demo/Quoridor.Demo/Quoridor.Demo.csproj`
Expected: Build succeeded, 0 错误。

- [ ] **Step 5: Commit**

```bash
git add demo/Quoridor.Demo/Quoridor.Demo.csproj demo/Quoridor.Demo/Program.cs
git commit -m "feat(demo): --acceptance 子命令骨架 + UI.Logic 引用"
```

---

## Task 6: 场景 1a 换位断言 + 场景 1b Kid 终局流

**Files:**
- Modify: `demo/Quoridor.Demo/Program.cs`

- [ ] **Step 1: 实现 Scenario1Swap**

把 `private static int Scenario1Swap() => 0;` 替换为:

```csharp
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
```

- [ ] **Step 2: 实现 Scenario1KidGame**

把 `private static int Scenario1KidGame() => 0;` 替换为:

```csharp
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
```

- [ ] **Step 3: 构建 + 跑 --acceptance(场景 2 仍占位)**

Run: `dotnet build demo/Quoridor.Demo/Quoridor.Demo.csproj && dotnet run --project demo/Quoridor.Demo -- --acceptance`
Expected: 场景 1a `[PASS]`、场景 1b `[PASS]`、场景 2 `[PASS]`(占位返回 0),末尾"验收全部 PASS"。

- [ ] **Step 4: Commit**

```bash
git add demo/Quoridor.Demo/Program.cs
git commit -m "feat(demo): 场景1a换位断言 + 1b Kid终局流"
```

---

## Task 7: 场景 2 设墙预览合法/非法

**Files:**
- Modify: `demo/Quoridor.Demo/Program.cs`

- [ ] **Step 1: 实现 Scenario2Preview + StepsOf helper**

把 `private static int Scenario2Preview() => 0;` 替换为:

```csharp
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
```

- [ ] **Step 2: 构建 + 跑 --acceptance**

Run: `dotnet build demo/Quoridor.Demo/Quoridor.Demo.csproj && dotnet run --project demo/Quoridor.Demo -- --acceptance`
Expected: 三场景全 `[PASS]`,末尾"验收全部 PASS",退出码 0。

- [ ] **Step 3: 验退出码**

Run: `dotnet run --project demo/Quoridor.Demo -- --acceptance > /dev/null 2>&1; echo "exit=$?"`
Expected: `exit=0`

- [ ] **Step 4: Commit**

```bash
git add demo/Quoridor.Demo/Program.cs
git commit -m "feat(demo): 场景2 设墙预览合法/非法"
```

---

## Task 8: 手动验收清单文档

**Files:**
- Create: `docs/superpowers/acceptance/2026-07-06-plan4-acceptance-checklist.md`

- [ ] **Step 1: 写清单**

创建文件,内容如下:

````markdown
# Plan 4 手动验收清单(2026-07-06)

> 7 项手动验收,对应 spec `2026-07-02-quoridor-ui-design.md` §9.2。第 1 项已过(Plan 4 验收项1)。CLI 无法验证视觉/鼠标交互,需真人跑游戏。

**启动**:`cd src/Quoridor.UI && godot-mono --path .`(须 mono 版)。

---

## 1. 人机标准 9×9,玩家1先手(已过)

走完一局至胜。✅ Plan 4 已验收。

## 2. 人机 Kid 7×7,玩家2先手(AI 先走),验证换位

- **操作**:StartFrame 选 "Kid 7×7" + "人机" + 任意难度 + "玩家2 先手" → 开始
- **期望视觉**:HUD 顶部 "先手: 玩家2";开局先出 "AI 思考中…"(灰),AI 落子后 "轮到 玩家1 走棋"(黄,人类=P2 显作玩家1)
- **期望日志**:`StartFrame: ... first=P2`、`MainController.StartSession ... first=P2`

## 3. hot-seat 两人交替走子与设墙

- **操作**:StartFrame 选 "双人 hot-seat" + 任意变体 → 开始 → 两人轮流点格子走子、点槽设墙
- **期望视觉**:大字号状态行在 "轮到 玩家1/玩家2 走棋" 间交替,配色黄↔蓝切换;投降按钮在人类回合可见可点
- **期望日志**:每手 `UI click cell ...` 或 `UI click slot ...` + 对应 `PawnMoved`/`WallPlaced`

## 4. 设墙悬浮预览:合法(绿)+路线+步数;非法(红)

- **操作**:人类回合,鼠标悬停一个**不切断路径**的墙槽 → 半透明绿色立柱 + 各棋子最短路线线 + 步数 Label3D;再悬停一个**切断对方路径**的墙槽 → 半透明红色立柱
- **期望视觉**:合法=绿+路线+步数;非法=红
- **期望日志**:预览纯视觉,无额外日志;若强行点击非法槽落子,footer 显示 "设墙被拒: <原因>"

## 5. 墙数耗尽后墙槽不可拾取

- **操作**:把一方墙用到 0 → 该方回合悬停任意墙槽
- **期望视觉**:墙预算显示 "玩家X: 0";悬停无预览立柱、点击无反应(槽 `InputRayPickable=false`)
- **期望日志**:无 `UI click slot` 日志(拾取被禁)

## 6. 终局提示胜者 + "回到开始页"可循环

- **操作**:走至一方到达目标边 → 大字号 "★ 玩家X 获胜! ★" + "回到开始页"按钮出现 → 点击 → 回 StartFrame → 可再开新局
- **期望视觉**:胜者配色(黄/蓝);home 按钮可见
- **期望日志**:`PlayerWon` 事件触发;切场景时 `MainController.EndSession`

## 7. 辅助模式预览在 mouse_exit 后清除

- **操作**:悬停墙槽显示预览 → 鼠标移开槽区
- **期望视觉**:立柱/路线线/步数 Label3D 立即消失
- **期望日志**:无(`MouseExited → SlotCleared → preview.Clear` 纯视觉)

---

**勾选**:_逐条核对,凡不符则记 bug 回 superpowers:systematic-debugging。_
````

- [ ] **Step 2: Commit**

```bash
git add docs/superpowers/acceptance/2026-07-06-plan4-acceptance-checklist.md
git commit -m "docs: Plan 4 手动验收清单(7 项, 带期望日志信号)"
```

---

## Task 9: 最终验证

**Files:** 无(纯跑命令)

- [ ] **Step 1: 全量测试**

Run: `dotnet test Quoridor.slnx`
Expected: Domain 74 + Application 41(+4 = 45) + UI.Logic 45 = 164 全绿。

- [ ] **Step 2: demo --acceptance**

Run: `dotnet run --project demo/Quoridor.Demo -- --acceptance`
Expected: 三场景全 `[PASS]`,"验收全部 PASS"。

- [ ] **Step 3: Godot 项目 headless 加载检查**

Run: `dotnet build src/Quoridor.UI/Quoridor.UI.csproj && godot-mono --headless --path src/Quoridor.UI --quit`
Expected: 加载无报错(spec §9.3 运行门槛)。若 `godot-mono` 不在 PATH,记为"环境受限,跳过",以 `dotnet build` 通过为构建门槛。

- [ ] **Step 4: 更新 CLAUDE.md 现状(可选,确认后再改)**

若全绿,把 `CLAUDE.md` 现状行的测试数 160→164,并在"后续"里把"验收项 2-7"标为已完成(手动部分留待用户勾清单)。

- [ ] **Step 5: 最终 Commit(若有 Step 4 改动)**

```bash
git add CLAUDE.md
git commit -m "docs: 现状更新—验收项2-7 收口(164 测试 + demo --acceptance)"
```

---

## Self-Review

**1. Spec coverage:**
- §3 表 #2 Kid P2换位 → Task 1(Kid legal)+ Task 2(Kid illegal)+ Task 6(1a换位断言)+ Task 8 清单#2。✅
- #3 hot-seat → Task 8 清单#3(逻辑层已覆盖,无新测试,符合 spec)。✅
- #4 预览合法/非法+步数 → Task 1/2/3 + Task 7 + 清单#4。✅
- #5 墙耗尽禁拾 → Task 4(文档化预览忽略墙数)+ 清单#5。✅
- #6 终局+循环 → Task 6(1b Kid终局流)+ 清单#6。✅
- #7 mouse_exit 清除 → 清单#7(纯 UI,spec 明确不自动化)。✅
- §4 四测试 → Task 1-4。✅
- §5 两场景 → Task 6/7(场景1拆 1a/1b)。✅
- §6 清单 → Task 8。✅
- §7 验证标准 → Task 9。✅

**2. Placeholder scan:** 无 TBD/TODO。Task 3 步数期望值带"若 FAIL 改字面量不改生产代码"的明确处置,非占位。Task 5 场景方法占位返回 0 是编译骨架,Task 6/7 实填。✅

**3. Type consistency:** `PreviewResult.Legal/Reason/Routes`、`RoutePreview.Pawn/Steps`、`SeatsBuilder.Build`、`SeatMap.ForFirstMove/ToDisplayNumber`、`GameConfig`、`MatchMode.VsAi`、`GameSession(BoardConfig, IPlayer[])`、`PawnMoved/WallPlaced/PlayerWon.Who`、`RejectReason.WallBlocksAllPaths`、`WallPos/Cell/WallOrient`、`IPlayer.Id/IsHuman`、`AiPlayerFactory.Create` —— 均与源码核实一致。`StepsOf` helper 在测试文件与 demo 各有一个 private(两文件不共享),签名一致。✅
