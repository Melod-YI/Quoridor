# Quoridor AI 实现计划（Plan 2）

> **状态：✅ 已完成并合入 master（commit 054b6ac，63 测试通过）。** 6 个任务全部 TDD 实现并经两阶段评审。

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 Quoridor.Domain 内新增 AI 子包，实现纯逻辑电脑玩家：`IQuoridorAi` 接口 + `GreedyAi`（1-ply 贪心）+ `MinimaxAi`（Alpha-Beta 剪枝，难度=搜索深度），复用 Plan 1 的 PathFinder 与合法动作集。

**Architecture:** AI 为 Domain 内新子包 `Quoridor.Domain.AI`（新增 `AI/` 目录），依赖已有的 `Core`/`Path`/`Rules`，不引入 Godot 或新外部依赖。`AiActionSet` 用 `MoveLegality.LegalTargets` + `WallLegality.Validate` 生成候选命令；`Evaluator` 用 `PathFinder.ShortestPath` 算双方最短步数差作评估；`GreedyAi` 1-ply 选最大评估；`MinimaxAi` Alpha-Beta + 1-ply 动作排序。AI 返回 `IGameCommand`（非 spec 的 `Move`，见下方偏离说明）。

**Tech Stack:** C# 14 / .NET 10 / xUnit / `System.Collections.Immutable`。

**所属设计：** `docs/superpowers/specs/2026-06-30-quoridor-design.md` §5.4。**前置：** Plan 1（Domain Core）已合入 master。

**偏离说明（相对 spec §5.4）：** spec 写 `IQuoridorAi.Choose(...) → Move`。Plan 1 未实现 `Move` 类型；`IGameCommand`（`MovePawnCommand`/`PlaceWallCommand`）结构等价。本计划让 AI 返回 `IGameCommand`，避免并行重复类型，Plan 3 的 `GameSession` 可直接 `Submit`。语义无损失。

---

## Prerequisites

- .NET 10 SDK（10.0.301 已装）。
- Plan 1 已在 master：`src/Quoridor.Domain/`（Core/Path/Rules/Notation）+ `tests/Quoridor.Domain.Tests/`，`dotnet test` 47 通过。
- 工作目录：仓库根。建议执行时用 `superpowers:using-git-worktrees` 建隔离工作树。

## 已有 API（Plan 1，本计划直接复用，勿改）

- `MoveLegality.LegalTargets(GameState) → ImmutableArray<Cell>`：当前活跃玩家合法走子目标格。
- `WallLegality.Validate(GameState, WallPos) → RejectReason?`：null 表示墙合法。
- `PathFinder.ShortestPath(BoardGraph, Cell, GoalEdge) → PathResult(int Distance, ImmutableArray<Cell> Path)`：Distance=-1 不可达。
- `BoardGraph(GameState)` 构造；`.Config`/`.InBounds`/`.HasWallBetween`/`.EdgesOf`。
- `RuleEngine.ValidateAndApply(GameState, IGameCommand) → ApplyResult(GameState? State, ImmutableArray<IGameEvent> Events)`：`State` 非空=已应用，null=拒绝。
- `GameState`（sealed record **class**）：`Config/Players/Pawns/Walls/ActivePlayer/Phase/Winner`，`PawnOf(id)`/`PlayerOf(id)`/`IsFinished`。**`GameState?` 是可空引用类型，没有 `.Value`**——取值用 `r.State!`。
- `PlayerState(PlayerId Id, Cell Start, GoalEdge Goal, int WallsLeft)`。
- `Commands`：`MovePawnCommand(Cell To)`、`PlaceWallCommand(WallPos Wall)`，均实现 `IGameCommand`。
- `ValueTypes`：`PlayerId`/`GoalEdge`/`WallOrient`/`Cell`/`WallPos`/`BoardConfig` 等。

## 踩坑提醒（来自 Plan 1）

- **不要写 `r.State!.Value`** —— `GameState` 是引用类型，`r.State!` 即可。
- **Domain 纯逻辑层不加日志** —— CLAUDE.md 的日志要求在 Plan 3 Application 层落实。
- **构造测试局面用 `with` + `ImmutableArrayExtensions.Replace`** 改棋子位置/墙数。
- **设墙测试注意 P2 起点**：P2 初始在 (4,8)（9×9）或 (3,6)（7×7 Kid），凡让 P1 走到该格获胜时先把 P2 挪开。

## 文件结构

```
src/Quoridor.Domain/AI/
  Difficulty.cs        -- 枚举 Easy/Medium/Hard
  IQuoridorAi.cs        -- 接口：IGameCommand Choose(GameState, Difficulty)
  AiActionSet.cs        -- 静态：生成当前玩家全部合法命令（走子+墙）
  Evaluator.cs          -- 静态：局面评估（对手总步数−自己步数，权重10 + 墙数差）
  GreedyAi.cs           -- 1-ply 贪心，选评估最大的命令
  MinimaxAi.cs          -- Alpha-Beta + 1-ply 动作排序，深度由 Difficulty 决定
tests/Quoridor.Domain.Tests/AI/
  AiActionSetTests.cs
  EvaluatorTests.cs
  GreedyAiTests.cs
  MinimaxAiTests.cs
  AiSelfPlayTests.cs
```

依赖方向：`AI` → `Core`/`Path`/`Rules`（同程序集，无新项目引用）。

## 评估函数与搜索约定

- `Evaluator.Evaluate(GameState, PlayerId ai) → int`：
  - `state.Winner == ai` → `+WinScore`(100000)；`Winner` 非空且 ≠ ai → `-WinScore`。
  - 否则：`(对手最短步数总和 − 自己最短步数) * 10 + (自己剩余墙数 − 对手剩余墙数总和)`。不可达按 1000 计。
- `MinimaxAi` 深度：`Easy=1 / Medium=2 / Hard=3`（plies）。
- 动作排序：每个节点按 1-ply 评估降序（max 节点）/升序（min 节点），提升 Alpha-Beta 剪枝。
- 多人（4P）简化：AI 为最大化方，所有非 AI 玩家视为最小化方（保守假设；2P 精确）。
- 性能：Hard 在 9×9 上较慢（墙分支大），测试用 Kid 7×7 或浅深度；真实对局建议 Easy/Medium。

---

## Task 1: Difficulty 枚举 + IQuoridorAi 接口

**Files:**
- Create: `src/Quoridor.Domain/AI/Difficulty.cs`
- Create: `src/Quoridor.Domain/AI/IQuoridorAi.cs`
- Test: `tests/Quoridor.Domain.Tests/AI/IQuoridorAiTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Domain.Tests/AI/IQuoridorAiTests.cs`：

```csharp
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Xunit;

namespace Quoridor.Domain.Tests.AI;

public class IQuoridorAiTests
{
    [Fact]
    public void Difficulty_enum_has_three_levels()
    {
        Assert.True(System.Enum.IsDefined(typeof(Difficulty), "Easy"));
        Assert.True(System.Enum.IsDefined(typeof(Difficulty), "Medium"));
        Assert.True(System.Enum.IsDefined(typeof(Difficulty), "Hard"));
    }

    [Fact]
    public void Stub_ai_returns_a_legal_command()
    {
        // 用一个最简 stub 验证接口契约：Choose 返回 IGameCommand
        IQuoridorAi ai = new StubAdvanceAi();
        var cmd = ai.Choose(GameSetup.CreateStandard2P(), Difficulty.Easy);
        Assert.IsType<MovePawnCommand>(cmd);
        Assert.Equal(new Cell(4, 1), ((MovePawnCommand)cmd).To);
    }

    private sealed class StubAdvanceAi : IQuoridorAi
    {
        public IGameCommand Choose(GameState state, Difficulty difficulty) =>
            new MovePawnCommand(new Cell(4, 1));
    }
}
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test`
Expected: FAIL（`Difficulty`/`IQuoridorAi` 未定义，CS0246）。

- [ ] **Step 3: 写实现**

`src/Quoridor.Domain/AI/Difficulty.cs`：

```csharp
namespace Quoridor.Domain.AI;

public enum Difficulty { Easy, Medium, Hard }
```

`src/Quoridor.Domain/AI/IQuoridorAi.cs`：

```csharp
using Quoridor.Domain.Core;

namespace Quoridor.Domain.AI;

public interface IQuoridorAi
{
    IGameCommand Choose(GameState state, Difficulty difficulty);
}
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test`
Expected: PASS（47 旧 + 2 新 = 49）。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(ai): 添加 Difficulty 枚举与 IQuoridorAi 接口"
```

---

## Task 2: AiActionSet（候选命令生成）

**Files:**
- Create: `src/Quoridor.Domain/AI/AiActionSet.cs`
- Test: `tests/Quoridor.Domain.Tests/AI/AiActionSetTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Domain.Tests/AI/AiActionSetTests.cs`：

```csharp
using System.Linq;
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Domain.Tests.AI;

public class AiActionSetTests
{
    [Fact]
    public void Generate_includes_legal_moves_and_walls()
    {
        var state = GameSetup.CreateStandard2P();
        var actions = AiActionSet.Generate(state);

        // 含至少一个走子（P1 北上到 (4,1)）与至少一面墙
        Assert.Contains(actions, a => a is MovePawnCommand m && m.To == new Cell(4, 1));
        Assert.Contains(actions, a => a is PlaceWallCommand);
        Assert.True(actions.Length > 1);
    }

    [Fact]
    public void All_generated_actions_are_legal()
    {
        var state = GameSetup.CreateStandard2P();
        var actions = AiActionSet.Generate(state);
        Assert.NotEmpty(actions);
        foreach (var a in actions)
        {
            var r = RuleEngine.ValidateAndApply(state, a);
            Assert.NotNull(r.State);  // AiActionSet 只产合法命令
        }
    }

    [Fact]
    public void Generate_omits_walls_when_none_left()
    {
        var state = GameSetup.CreateStandard2P();
        var p1 = state.PlayerOf(PlayerId.P1);
        state = state with
        {
            Players = state.Players.Replace(p1, p1 with { WallsLeft = 0 }),
        };
        var actions = AiActionSet.Generate(state);
        Assert.DoesNotContain(actions, a => a is PlaceWallCommand);
        Assert.Contains(actions, a => a is MovePawnCommand);
    }
}
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test`
Expected: FAIL（`AiActionSet` 未定义）。

- [ ] **Step 3: 写实现**

`src/Quoridor.Domain/AI/AiActionSet.cs`：

```csharp
using System.Collections.Generic;
using System.Collections.Immutable;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;

namespace Quoridor.Domain.AI;

public static class AiActionSet
{
    public static ImmutableArray<IGameCommand> Generate(GameState state)
    {
        var actions = new List<IGameCommand>();

        // 走子：复用 MoveLegality 的合法目标格
        foreach (var to in MoveLegality.LegalTargets(state))
            actions.Add(new MovePawnCommand(to));

        // 设墙：枚举所有 anchor×朝向，仅保留 WallLegality 判定合法的
        var player = state.PlayerOf(state.ActivePlayer);
        if (player.WallsLeft > 0)
        {
            var cfg = state.Config;
            for (int c = 0; c < cfg.MaxIndex; c++)
                for (int r = 0; r < cfg.MaxIndex; r++)
                {
                    var anchor = new Cell(c, r);
                    var h = new WallPos(anchor, WallOrient.Horizontal);
                    if (WallLegality.Validate(state, h) is null)
                        actions.Add(new PlaceWallCommand(h));
                    var v = new WallPos(anchor, WallOrient.Vertical);
                    if (WallLegality.Validate(state, v) is null)
                        actions.Add(new PlaceWallCommand(v));
                }
        }
        return actions.ToImmutableArray();
    }
}
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test`
Expected: PASS（49 + 3 = 52）。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(ai): 添加 AiActionSet 候选命令生成(走子+合法墙)"
```

---

## Task 3: Evaluator（局面评估）

**Files:**
- Create: `src/Quoridor.Domain/AI/Evaluator.cs`
- Test: `tests/Quoridor.Domain.Tests/AI/EvaluatorTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Domain.Tests/AI/EvaluatorTests.cs`：

```csharp
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Xunit;

namespace Quoridor.Domain.Tests.AI;

public class EvaluatorTests
{
    [Fact]
    public void Initial_2p_is_zero()
    {
        // P1/P2 各 dist=8，墙数相等 → (8-8)*10 + (10-10) = 0
        var state = GameSetup.CreateStandard2P();
        Assert.Equal(0, Evaluator.Evaluate(state, PlayerId.P1));
        Assert.Equal(0, Evaluator.Evaluate(state, PlayerId.P2));
    }

    [Fact]
    public void Closer_p1_scores_higher()
    {
        // P1 放到 (4,4)（距北 4 步），P2 仍在 (4,8)（距南 8 步）
        var state = PlaceP1At(GameSetup.CreateStandard2P(), new Cell(4, 4));
        // (8-4)*10 + (10-10) = 40
        Assert.Equal(40, Evaluator.Evaluate(state, PlayerId.P1));
    }

    [Fact]
    public void Winner_returns_win_score()
    {
        var state = GameSetup.CreateStandard2P() with
        {
            Phase = Phase.Finished,
            Winner = PlayerId.P1,
        };
        Assert.Equal(Evaluator.WinScore, Evaluator.Evaluate(state, PlayerId.P1));
        Assert.Equal(-Evaluator.WinScore, Evaluator.Evaluate(state, PlayerId.P2));
    }

    private static GameState PlaceP1At(GameState s, Cell c)
    {
        var p1 = s.PawnOf(PlayerId.P1);
        return s with { Pawns = s.Pawns.Replace(p1, p1 with { Pos = c }) };
    }
}
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test`
Expected: FAIL（`Evaluator` 未定义）。

- [ ] **Step 3: 写实现**

`src/Quoridor.Domain/AI/Evaluator.cs`：

```csharp
using Quoridor.Domain.Core;
using Quoridor.Domain.Path;

namespace Quoridor.Domain.AI;

public static class Evaluator
{
    public const int WinScore = 100_000;
    private const int Unreachable = 1_000;

    public static int Evaluate(GameState state, PlayerId ai)
    {
        if (state.Winner is { } w)
            return w == ai ? WinScore : -WinScore;

        var graph = new BoardGraph(state);
        int myDist = DistOrUnreachable(graph, state.PawnOf(ai), state.PlayerOf(ai).Goal);

        int oppTotal = 0;
        int oppWallsTotal = 0;
        foreach (var p in state.Players)
        {
            if (p.Id == ai) continue;
            oppTotal += DistOrUnreachable(graph, state.PawnOf(p.Id), p.Goal);
            oppWallsTotal += p.WallsLeft;
        }

        int myWalls = state.PlayerOf(ai).WallsLeft;
        return (oppTotal - myDist) * 10 + (myWalls - oppWallsTotal);
    }

    private static int DistOrUnreachable(BoardGraph g, Pawn pawn, GoalEdge goal)
    {
        int d = PathFinder.ShortestPath(g, pawn.Pos, goal).Distance;
        return d < 0 ? Unreachable : d;
    }
}
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test`
Expected: PASS（52 + 3 = 55）。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(ai): 添加 Evaluator 局面评估(步数差+墙数差)"
```

---

## Task 4: GreedyAi（1-ply 贪心）

**Files:**
- Create: `src/Quoridor.Domain/AI/GreedyAi.cs`
- Test: `tests/Quoridor.Domain.Tests/AI/GreedyAiTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Domain.Tests/AI/GreedyAiTests.cs`：

```csharp
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Domain.Tests.AI;

public class GreedyAiTests
{
    [Fact]
    public void Chooses_advancing_move_on_empty_board()
    {
        var state = GameSetup.CreateStandard2P();
        var cmd = new GreedyAi().Choose(state, Difficulty.Easy);
        var move = Assert.IsType<MovePawnCommand>(cmd);
        // P1 目标是北（row 增大），贪心选最大评估=向北推进
        Assert.True(move.To.Row > 0, $"期望向北推进，实际 To={move.To}");
    }

    [Fact]
    public void Never_returns_illegal_command()
    {
        var state = GameSetup.CreateStandard2P();
        foreach (Difficulty d in new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard })
        {
            var cmd = new GreedyAi().Choose(state, d);
            var r = RuleEngine.ValidateAndApply(state, cmd);
            Assert.NotNull(r.State);
        }
    }

    [Fact]
    public void Returns_move_when_no_walls_left()
    {
        var state = GameSetup.CreateStandard2P();
        var p1 = state.PlayerOf(PlayerId.P1);
        state = state with
        {
            Players = state.Players.Replace(p1, p1 with { WallsLeft = 0 }),
        };
        var cmd = new GreedyAi().Choose(state, Difficulty.Easy);
        Assert.IsType<MovePawnCommand>(cmd);
    }
}
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test`
Expected: FAIL（`GreedyAi` 未定义）。

- [ ] **Step 3: 写实现**

`src/Quoridor.Domain/AI/GreedyAi.cs`：

```csharp
using System;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;

namespace Quoridor.Domain.AI;

public sealed class GreedyAi : IQuoridorAi
{
    public IGameCommand Choose(GameState state, Difficulty difficulty)
    {
        var actions = AiActionSet.Generate(state);
        if (actions.Length == 0)
            throw new InvalidOperationException("无合法动作（Quoridor 中玩家总有合法走子，不应发生）");

        var me = state.ActivePlayer;
        IGameCommand best = actions[0];
        int bestScore = int.MinValue;
        foreach (var a in actions)
        {
            var r = RuleEngine.ValidateAndApply(state, a);
            if (r.State is null) continue; // AiActionSet 已过滤，理论不触发
            int score = Evaluator.Evaluate(r.State, me);
            if (score > bestScore)
            {
                bestScore = score;
                best = a;
            }
        }
        return best;
    }
}
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test`
Expected: PASS（55 + 3 = 58）。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(ai): 添加 GreedyAi 1-ply 贪心电脑玩家"
```

---

## Task 5: MinimaxAi（Alpha-Beta + 动作排序）

**Files:**
- Create: `src/Quoridor.Domain/AI/MinimaxAi.cs`
- Test: `tests/Quoridor.Domain.Tests/AI/MinimaxAiTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Domain.Tests/AI/MinimaxAiTests.cs`：

```csharp
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Domain.Tests.AI;

public class MinimaxAiTests
{
    [Fact]
    public void Never_returns_illegal_command()
    {
        var state = GameSetup.CreateStandard2P();
        var cmd = new MinimaxAi().Choose(state, Difficulty.Easy);
        var r = RuleEngine.ValidateAndApply(state, cmd);
        Assert.NotNull(r.State);
    }

    [Fact]
    public void Takes_immediate_win_when_available()
    {
        // 9×9：P1 在 (4,7)，P2 挪到 (3,8) 让出 (4,8)；P1 一步到 (4,8) 即胜
        var state = MoveP1To(MoveP2To(GameSetup.CreateStandard2P(), new Cell(3, 8)), new Cell(4, 7));
        var cmd = new MinimaxAi().Choose(state, Difficulty.Easy);
        var move = Assert.IsType<MovePawnCommand>(cmd);
        Assert.Equal(new Cell(4, 8), move.To);
    }

    [Fact]
    public void Blocks_opponent_one_step_from_win()
    {
        // Kid 7×7：P1 在 (3,3)，P2 在 (3,1)（距南端 row0 一步），P1 回合。
        // 若 P1 推进，P2 下一手到 (3,0) 获胜 → 评估 -WinScore；
        // MinimaxAi(Medium, depth=2) 看到这一点，会选一面阻断 P2 的墙而非推进。
        var state = MoveP1To(MoveP2To(GameSetup.CreateKid2P(), new Cell(3, 1)), new Cell(3, 3));
        var cmd = new MinimaxAi().Choose(state, Difficulty.Medium);
        Assert.IsType<PlaceWallCommand>(cmd);
        // 且该墙合法
        var r = RuleEngine.ValidateAndApply(state, cmd);
        Assert.NotNull(r.State);
    }

    private static GameState MoveP1To(GameState s, Cell c)
    {
        var p1 = s.PawnOf(PlayerId.P1);
        return s with { Pawns = s.Pawns.Replace(p1, p1 with { Pos = c }) };
    }
    private static GameState MoveP2To(GameState s, Cell c)
    {
        var p2 = s.PawnOf(PlayerId.P2);
        return s with { Pawns = s.Pawns.Replace(p2, p2 with { Pos = c }) };
    }
}
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test`
Expected: FAIL（`MinimaxAi` 未定义）。

- [ ] **Step 3: 写实现**

`src/Quoridor.Domain/AI/MinimaxAi.cs`：

```csharp
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;

namespace Quoridor.Domain.AI;

public sealed class MinimaxAi : IQuoridorAi
{
    public IGameCommand Choose(GameState state, Difficulty difficulty)
    {
        int depth = difficulty switch
        {
            Difficulty.Easy => 1,
            Difficulty.Medium => 2,
            Difficulty.Hard => 3,
            _ => 1,
        };

        var me = state.ActivePlayer;
        var actions = Order(state, AiActionSet.Generate(state), me, descending: true);
        if (actions.Length == 0)
            throw new InvalidOperationException("无合法动作");

        IGameCommand best = actions[0];
        int bestScore = int.MinValue;
        int alpha = int.MinValue;
        int beta = int.MaxValue;
        foreach (var a in actions)
        {
            var r = RuleEngine.ValidateAndApply(state, a);
            if (r.State is null) continue;
            int score = AlphaBeta(r.State, depth - 1, alpha, beta, me);
            if (score > bestScore)
            {
                bestScore = score;
                best = a;
            }
            if (score > alpha) alpha = score;
        }
        return best;
    }

    private static int AlphaBeta(GameState state, int depth, int alpha, int beta, PlayerId me)
    {
        if (depth <= 0 || state.IsFinished)
            return Evaluator.Evaluate(state, me);

        bool maximizing = state.ActivePlayer == me;
        var actions = Order(state, AiActionSet.Generate(state), me, descending: maximizing);
        int value = maximizing ? int.MinValue : int.MaxValue;

        foreach (var a in actions)
        {
            var r = RuleEngine.ValidateAndApply(state, a);
            if (r.State is null) continue;
            int child = AlphaBeta(r.State, depth - 1, alpha, beta, me);

            if (maximizing)
            {
                if (child > value) value = child;
                if (value > alpha) alpha = value;
                if (alpha >= beta) break;
            }
            else
            {
                if (child < value) value = child;
                if (value < beta) beta = value;
                if (alpha >= beta) break;
            }
        }
        return value;
    }

    /// <summary>1-ply 评估排序：max 节点降序、min 节点升序，提升剪枝。</summary>
    private static ImmutableArray<IGameCommand> Order(
        GameState state, ImmutableArray<IGameCommand> actions, PlayerId me, bool descending)
    {
        var scored = new List<(IGameCommand Cmd, int Score)>(actions.Length);
        foreach (var a in actions)
        {
            var r = RuleEngine.ValidateAndApply(state, a);
            int s = r.State is null ? int.MinValue : Evaluator.Evaluate(r.State, me);
            scored.Add((a, s));
        }
        var ordered = descending
            ? scored.OrderByDescending(t => t.Score)
            : scored.OrderBy(t => t.Score);
        return ordered.Select(t => t.Cmd).ToImmutableArray();
    }
}
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test`
Expected: PASS（58 + 3 = 61）。`Blocks_opponent_one_step_from_win` 用 Kid 7×7 + Medium(depth2)，应 <1s。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(ai): 添加 MinimaxAi Alpha-Beta 剪枝电脑玩家(难度=深度)"
```

---

## Task 6: AI 自对弈集成测试 + README 更新

**Files:**
- Create: `tests/Quoridor.Domain.Tests/AI/AiSelfPlayTests.cs`
- Modify: `src/Quoridor.Domain/README.md`（追加 AI 段）

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Domain.Tests/AI/AiSelfPlayTests.cs`：

```csharp
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Domain.Tests.AI;

public class AiSelfPlayTests
{
    [Fact]
    public void Greedy_self_play_terminates_with_winner()
    {
        var state = GameSetup.CreateStandard2P();
        var ai = new GreedyAi();
        int plies = 0;
        while (!state.IsFinished && plies < 400)
        {
            var cmd = ai.Choose(state, Difficulty.Easy);
            var r = RuleEngine.ValidateAndApply(state, cmd);
            Assert.NotNull(r.State);  // AI 永不下非法手
            state = r.State!;
            plies++;
        }
        Assert.True(state.IsFinished, $"Greedy 自对弈 {plies} 手未终止");
        Assert.NotNull(state.Winner);
    }

    [Fact]
    public void Minimax_easy_self_play_terminates_on_kid()
    {
        var state = GameSetup.CreateKid2P();
        var ai = new MinimaxAi();
        int plies = 0;
        while (!state.IsFinished && plies < 300)
        {
            var cmd = ai.Choose(state, Difficulty.Easy);
            var r = RuleEngine.ValidateAndApply(state, cmd);
            Assert.NotNull(r.State);
            state = r.State!;
            plies++;
        }
        Assert.True(state.IsFinished, $"Minimax(Easy) 自对弈 {plies} 手未终止");
        Assert.NotNull(state.Winner);
    }
}
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test`
Expected: FAIL（`AiSelfPlayTests` 类不存在 / 编译错误）。

- [ ] **Step 3: 运行并验证通过**

Run: `dotnet test`
Expected: PASS（61 + 2 = 63）。Greedy 自对弈应 <1s；Minimax(Easy) Kid 自对弈应 <2s。若超时，先确认是否深度/分支问题，**不改测试迎合**——报 DONE_WITH_CONCERNS 说明。

- [ ] **Step 4: 更新 README**

在 `src/Quoridor.Domain/README.md` 的"职责"列表末尾追加：

```markdown
- `AI`：`IQuoridorAi` 接口、`GreedyAi`（1-ply 贪心）、`MinimaxAi`（Alpha-Beta，难度=深度）、`AiActionSet`（候选命令）、`Evaluator`（步数差评估）。复用 Path/Rules，纯逻辑、不依赖 Godot。
```

并在"唯一状态变更入口"段下方追加：

```markdown
## AI 用法

```
var ai = new MinimaxAi();
IGameCommand cmd = ai.Choose(state, Difficulty.Medium);
var r = RuleEngine.ValidateAndApply(state, cmd);   // AI 永不下非法手
```
```

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "test(ai): 添加 AI 自对弈集成测试与 README 更新"
```

---

## Self-Review（计划自检结果）

**1. Spec 覆盖（spec §5.4）：**
- `IQuoridorAi` 接口 → Task 1。
- `GreedyAi`（最短路贪心 + 偶尔设墙）→ Task 4（1-ply 评估最大化，含设墙候选）。
- `MinimaxAi` + Alpha-Beta，难度=深度，评估复用 PathFinder → Task 3（Evaluator）+ Task 5（Minimax）。
- 动作排序提升剪枝 → Task 5 `Order`。
- 候选动作复用规则引擎合法集 → Task 2 `AiActionSet`（用 `MoveLegality`/`WallLegality`），AI 永不非法 → Task 4/5/6 测试覆盖。
- 偏离：AI 返回 `IGameCommand` 而非 `Move`（已在偏离说明记录，语义等价）。

**2. 占位符扫描：** 无 TBD/TODO；所有步骤含完整代码与测试。

**3. 类型一致性：**
- `IQuoridorAi.Choose(GameState, Difficulty) → IGameCommand`：Task 1 定义，Task 4/5 实现，Task 6 调用，签名一致。
- `AiActionSet.Generate(GameState) → ImmutableArray<IGameCommand>`：Task 2 定义，Task 4/5 调用一致。
- `Evaluator.Evaluate(GameState, PlayerId) → int` + `Evaluator.WinScore`：Task 3 定义，Task 4/5/测试调用一致。
- `Difficulty { Easy, Medium, Hard }`：Task 1 定义，Task 5 深度映射引用一致。
- 测试中 `r.State!`（无 `.Value`）——遵守 Plan 1 踩坑。
- `Replace` 扩展（Task 1 测试用 `state.Players.Replace`/`state.Pawns.Replace`）——Plan 1 已有 `ImmutableArrayExtensions.Replace<T> where T:class`，`Pawn`/`PlayerState` 为 sealed record class，匹配。

**4. 性能注意：** `MinimaxAi` Hard(9×9) 较慢；测试用 Kid 7×7 或 Easy。已在本计划与 README 标注。

---

## 执行交接

计划已保存到 `docs/superpowers/plans/2026-06-30-quoridor-ai.md`。两种执行方式：

1. **Subagent-Driven（推荐）** —— 每任务派独立子 agent + 两阶段评审。
2. **Inline Execution** —— 当前会话批量执行 + 检查点。

选哪种？
