# Quoridor Domain Core 实现计划（Plan 1）

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 实现稳定、零 Godot 依赖、可独立单测的 Quoridor Domain 核心类库（不可变 GameState + 命令/事件、规则引擎、BFS 路径与可达性、Modern Algebraic Notation 编/解码）。

**Architecture:** Domain 为纯 C# 类库（net10.0），内部按职责分包：`Core`（数据模型/命令/事件）、`Rules`（合法性 + RuleEngine）、`Path`（无向图 + BFS + 可达性）、`Notation`（记谱）。状态不可变，唯一变更入口 `RuleEngine.ValidateAndApply(state, command) → (GameState?, IGameEvent[])`。配套 xUnit 测试项目，`dotnet test` 即可跑，无需图形环境。

**Tech Stack:** C# 14 / .NET 10 / xUnit / `System.Collections.Immutable`。

**所属设计：** `docs/superpowers/specs/2026-06-30-quoridor-design.md`

**本计划范围（Plan 1）：** 仅 Domain 核心。不含 AI（Plan 2）、Application（Plan 3）、Godot UI（Plan 4）。

---

## Prerequisites

- 安装 .NET 10 SDK（LTS，支持至 2028-11；下载 <https://dotnet.microsoft.com/download>）。验证：`dotnet --version` 输出 `10.x`。
  - 选型依据：Godot 4.6 的官方 C# 包 `GodotSharp 4.6.0` 兼容矩阵已显式列出 `net10.0`；.NET 8 将于 2026-11 EOL，新项目直接用 .NET 10 LTS。
- 本计划不需要 Godot 引擎；Godot 4.6 在 Plan 4 才需要（届时安装 Godot .NET 版）。
- 工作目录：仓库根 `C:\workspace\Quoridor`。
- 分支：建议在执行时通过 `superpowers:using-git-worktrees` 建立隔离工作树；否则在当前分支按任务频繁提交。
- 未来约束（不阻塞本计划）：Godot 4.x 的 C# 项目暂不支持 web 导出；移动端 Android/iOS 导出可用（Android 需 .NET 9+，10 满足）。

## 文件结构

执行完本计划后，仓库结构如下（每个文件单一职责）：

```
Quoridor.sln
src/Quoridor.Domain/Quoridor.Domain.csproj
src/Quorido.Domain/Core/
  Cell.cs                  -- 格子坐标（含比较，用于 Passage 规范化）
  ValueTypes.cs            -- WallOrient/PlayerId/GoalEdge/Phase/MoveKind/RejectReason/BoardVariant 枚举
  WallPos.cs               -- 墙位置（基准格 + 朝向）
  BoardConfig.cs           -- 棋盘配置 + WallBudget 静态助手
  Passage.cs               -- 规范化的相邻格通道（用于图与墙重叠判定）
  Pawn.cs / PlayerState.cs / GameState.cs
  GameSetup.cs             -- 初始局面工厂（标准2/4人、Kid2/4人）
  GoalChecker.cs           -- 是否到达目标边
  Commands.cs              -- IGameCommand / MovePawnCommand / PlaceWallCommand
  Events.cs                -- IGameEvent 及各事件 record
  ImmutableArrayExtensions.cs -- Replace 扩展
src/Quoridor.Domain/Path/
  Directions.cs            -- 四方向 + 垂直方向集
  BoardGraph.cs            -- 由 GameState 构造：HasWallBetween / InBounds / EdgesOf
  PathFinder.cs            -- BFS 最短路径
  Reachability.cs          -- 所有玩家是否仍可达目标
src/Quoridor.Domain/Rules/
  MoveLegality.cs          -- 当前玩家合法走子目标格集合（含直跳/斜跳）
  WallLegality.cs          -- 墙合法性（越界/重叠/可达性）
  RuleEngine.cs            -- ValidateAndApply 总入口
src/Quoridor.Domain/Notation/
  NotationService.cs       -- Encode / Decode / Replay
tests/Quoridor.Domain.Tests/Quoridor.Domain.Tests.csproj
  Core/CellTests.cs, Core/BoardConfigTests.cs, Core/GameSetupTests.cs, Core/GameStateTests.cs
  Path/BoardGraphTests.cs, Path/PathFinderTests.cs, Path/ReachabilityTests.cs
  Rules/MoveLegalityTests.cs, Rules/WallLegalityTests.cs, Rules/RuleEngineTests.cs
  Notation/NotationServiceTests.cs
  Cases/  -- 回归用例集
```

依赖方向：`Rules` 与 `Path` 与 `Notation` 都只依赖 `Core`；`Core` 不依赖任何上层。

## 命名与坐标约定（实现期统一）

- 内部坐标 0 基索引 `(Col, Row)`：左下角 `(0,0)` = 记谱 `a1`；`Col` 增大→东，`Row` 增大→北。
- 记谱互转：列字母 `a..` 对应 `Col`；行数字 `1..` 对应 `Row+1`。
- 墙基准格 = 该墙所触 4 格中最靠 a1 的格（最小 Col，再最小 Row）。墙跨越 2 列 × 2 行区域，`Anchor.Col/Row ∈ [0, MaxIndex-1]`。
- GoalEdge：`North`→`Row==MaxIndex`；`South`→`Row==0`；`West`→`Col==0`；`East`→`Col==MaxIndex`。
- 初始位置：2 人 P1`(mid,0)`/北、P2`(mid,Max)`/南；4 人 P1`(mid,0)`/北、P2`(0,mid)`/东、P3`(mid,Max)`/南、P4`(Max,mid)`/西。`mid = MaxIndex/2`。

---

## Task 1: 解决方案与项目脚手架

**Files:**
- Create: `Quoridor.sln`
- Create: `src/Quoridor.Domain/Quoridor.Domain.csproj`
- Create: `tests/Quoridor.Domain.Tests/Quoridor.Domain.Tests.csproj`
- Create: `.gitignore`

- [ ] **Step 1: 创建解决方案与项目**

```bash
dotnet new sln -n Quoridor
dotnet new classlib -n Quoridor.Domain -o src/Quoridor.Domain --framework net10.0
dotnet new xunit -n Quoridor.Domain.Tests -o tests/Quoridor.Domain.Tests --framework net10.0
dotnet sln add src/Quoridor.Domain/Quoridor.Domain.csproj
dotnet sln add tests/Quoridor.Domain.Tests/Quoridor.Domain.Tests.csproj
cd tests/Quoridor.Domain.Tests && dotnet add reference ../../src/Quoridor.Domain/Quoridor.Domain.csproj && cd ../..
```

删除 `src/Quoridor.Domain/Class1.cs` 与 `tests/Quoridor.Domain.Tests/UnitTest1.cs`（模板默认文件）：

```bash
rm src/Quoridor.Domain/Class1.cs tests/Quoridor.Domain.Tests/UnitTest1.cs
```

- [ ] **Step 2: 写 .gitignore**

创建 `.gitignore`：

```
bin/
obj/
*.user
.vs/
```

- [ ] **Step 3: 写一个冒烟测试，验证测试链路**

创建 `tests/Quoridor.Domain.Tests/SmokeTests.cs`：

```csharp
using Xunit;

namespace Quoridor.Domain.Tests;

public class SmokeTests
{
    [Fact]
    public void TestHarnessRuns()
    {
        Assert.Equal(4, 2 + 2);
    }
}
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test`
Expected: PASS（1 test）。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "chore: 初始化 .NET 解决方案与 Domain/Test 项目脚手架"
```

---

## Task 2: Core 值类型（Cell / 枚举 / WallPos / BoardConfig / Passage）

**Files:**
- Create: `src/Quoridor.Domain/Core/Cell.cs`
- Create: `src/Quoridor.Domain/Core/ValueTypes.cs`
- Create: `src/Quoridor.Domain/Core/WallPos.cs`
- Create: `src/Quoridor.Domain/Core/BoardConfig.cs`
- Create: `src/Quoridor.Domain/Core/Passage.cs`
- Test: `tests/Quoridor.Domain.Tests/Core/CellTests.cs`

- [ ] **Step 1: 写失败测试**

创建 `tests/Quoridor.Domain.Tests/Core/CellTests.cs`：

```csharp
using Quoridor.Domain.Core;
using Xunit;

namespace Quoridor.Domain.Tests.Core;

public class CellTests
{
    [Fact]
    public void CompareTo_orders_by_col_then_row()
    {
        Assert.True(new Cell(0, 5).CompareTo(new Cell(1, 0)) < 0);
        Assert.True(new Cell(2, 3).CompareTo(new Cell(2, 5)) < 0);
    }

    [Fact]
    public void WallPos_anchor_and_orient_roundtrip()
    {
        var w = new WallPos(new Cell(4, 2), WallOrient.Vertical);
        Assert.Equal(new Cell(4, 2), w.Anchor);
        Assert.Equal(WallOrient.Vertical, w.Orient);
    }

    [Fact]
    public void BoardConfig_standard_and_kid_sizes()
    {
        Assert.Equal(9, BoardConfig.Standard.Size);
        Assert.Equal(7, BoardConfig.Kid.Size);
        Assert.Equal(8, BoardConfig.Standard.MaxIndex);
    }

    [Fact]
    public void WallBudget_per_player()
    {
        Assert.Equal(10, WallBudget.PerPlayer(BoardVariant.Standard, 2));
        Assert.Equal(5, WallBudget.PerPlayer(BoardVariant.Standard, 4));
        Assert.Equal(8, WallBudget.PerPlayer(BoardVariant.Kid, 2));
        Assert.Equal(4, WallBudget.PerPlayer(BoardVariant.Kid, 4));
    }

    [Fact]
    public void Passage_canonical_orders_cells()
    {
        var p = Passage.Between(new Cell(3, 4), new Cell(2, 4));
        Assert.Equal(new Cell(2, 4), p.A);
        Assert.Equal(new Cell(3, 4), p.B);
    }
}
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test`
Expected: FAIL（类型未找到 / 编译错误）。

- [ ] **Step 3: 写实现**

`src/Quoridor.Domain/Core/Cell.cs`：

```csharp
namespace Quoridor.Domain.Core;

public readonly record struct Cell(int Col, int Row) : IComparable<Cell>
{
    public int CompareTo(Cell other)
    {
        int c = Col.CompareTo(other.Col);
        return c != 0 ? c : Row.CompareTo(other.Row);
    }
    public static bool operator <(Cell a, Cell b) => a.CompareTo(b) < 0;
    public static bool operator >(Cell a, Cell b) => a.CompareTo(b) > 0;
    public static bool operator <=(Cell a, Cell b) => a.CompareTo(b) <= 0;
    public static bool operator >=(Cell a, Cell b) => a.CompareTo(b) >= 0;
}
```

`src/Quoridor.Domain/Core/ValueTypes.cs`：

```csharp
namespace Quoridor.Domain.Core;

public enum WallOrient { Horizontal, Vertical }
public enum PlayerId { P1, P2, P3, P4 }
public enum GoalEdge { North, South, West, East }
public enum Phase { Running, Finished }
public enum MoveKind { Step, Jump, DiagonalJump }
public enum BoardVariant { Standard, Kid }

public enum RejectReason
{
    NotYourTurn,
    IllegalMove,
    BlockedByWall,
    OffBoard,
    WallOverlap,
    WallOutOfBounds,
    WallBlocksAllPaths,
    NoWallsLeft,
    GameFinished,
}
```

`src/Quoridor.Domain/Core/WallPos.cs`：

```csharp
namespace Quoridor.Domain.Core;

public readonly record struct WallPos(Cell Anchor, WallOrient Orient);
```

`src/Quoridor.Domain/Core/BoardConfig.cs`：

```csharp
namespace Quoridor.Domain.Core;

public sealed record BoardConfig(int Size, BoardVariant Variant)
{
    public static BoardConfig Standard => new(9, BoardVariant.Standard);
    public static BoardConfig Kid => new(7, BoardVariant.Kid);
    public int MaxIndex => Size - 1;
}

public static class WallBudget
{
    public static int PerPlayer(BoardVariant variant, int playerCount) => variant switch
    {
        BoardVariant.Standard => playerCount == 2 ? 10 : 5,
        BoardVariant.Kid => playerCount == 2 ? 8 : 4,
        _ => throw new System.ArgumentOutOfRangeException(nameof(variant)),
    };
}
```

`src/Quoridor.Domain/Core/Passage.cs`：

```csharp
namespace Quoridor.Domain.Core;

public readonly record struct Passage(Cell A, Cell B)
{
    public static Passage Between(Cell x, Cell y) =>
        x.CompareTo(y) <= 0 ? new Passage(x, y) : new Passage(y, x);
}
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test`
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(domain): 添加 Core 值类型 Cell/WallPos/BoardConfig/Passage"
```

---

## Task 3: Pawn / PlayerState / GameState / GameSetup / GoalChecker

**Files:**
- Create: `src/Quoridor.Domain/Core/Pawn.cs`
- Create: `src/Quoridor.Domain/Core/PlayerState.cs`
- Create: `src/Quoridor.Domain/Core/GameState.cs`
- Create: `src/Quoridor.Domain/Core/GameSetup.cs`
- Create: `src/Quoridor.Domain/Core/GoalChecker.cs`
- Create: `src/Quoridor.Domain/Core/ImmutableArrayExtensions.cs`
- Test: `tests/Quoridor.Domain.Tests/Core/GameSetupTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Domain.Tests/Core/GameSetupTests.cs`：

```csharp
using Quoridor.Domain.Core;
using Xunit;

namespace Quoridor.Domain.Tests.Core;

public class GameSetupTests
{
    [Fact]
    public void Standard2P_starting_positions_and_walls()
    {
        var s = GameSetup.CreateStandard2P();
        Assert.Equal(2, s.Players.Length);
        Assert.Equal(new Cell(4, 0), s.PawnOf(PlayerId.P1).Pos);
        Assert.Equal(new Cell(4, 8), s.PawnOf(PlayerId.P2).Pos);
        Assert.Equal(GoalEdge.North, s.PlayerOf(PlayerId.P1).Goal);
        Assert.Equal(GoalEdge.South, s.PlayerOf(PlayerId.P2).Goal);
        Assert.Equal(10, s.PlayerOf(PlayerId.P1).WallsLeft);
        Assert.Equal(PlayerId.P1, s.ActivePlayer);
        Assert.Equal(Phase.Running, s.Phase);
        Assert.Null(s.Winner);
    }

    [Fact]
    public void Standard4P_starting_positions()
    {
        var s = GameSetup.CreateStandard4P();
        Assert.Equal(4, s.Players.Length);
        Assert.Equal(new Cell(4, 0), s.PawnOf(PlayerId.P1).Pos);
        Assert.Equal(new Cell(0, 4), s.PawnOf(PlayerId.P2).Pos);
        Assert.Equal(new Cell(4, 8), s.PawnOf(PlayerId.P3).Pos);
        Assert.Equal(new Cell(8, 4), s.PawnOf(PlayerId.P4).Pos);
        Assert.Equal(GoalEdge.East, s.PlayerOf(PlayerId.P2).Goal);
        Assert.Equal(GoalEdge.West, s.PlayerOf(PlayerId.P4).Goal);
        Assert.Equal(5, s.PlayerOf(PlayerId.P1).WallsLeft);
    }

    [Fact]
    public void Kid2P_uses_smaller_board_and_walls()
    {
        var s = GameSetup.CreateKid2P();
        Assert.Equal(7, s.Config.Size);
        Assert.Equal(new Cell(3, 0), s.PawnOf(PlayerId.P1).Pos);
        Assert.Equal(8, s.PlayerOf(PlayerId.P1).WallsLeft);
    }

    [Fact]
    public void Kid4P_walls_per_player_is_four()
    {
        var s = GameSetup.CreateKid4P();
        Assert.Equal(4, s.PlayerOf(PlayerId.P1).WallsLeft);
    }

    [Fact]
    public void GoalChecker_detects_north_and_south()
    {
        var cfg = BoardConfig.Standard;
        Assert.True(GoalChecker.Reached(GoalEdge.North, new Cell(4, 8), cfg));
        Assert.False(GoalChecker.Reached(GoalEdge.North, new Cell(4, 7), cfg));
        Assert.True(GoalChecker.Reached(GoalEdge.South, new Cell(4, 0), cfg));
    }
}
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test`
Expected: FAIL（编译错误：类型未定义）。

- [ ] **Step 3: 写实现**

`src/Quoridor.Domain/Core/Pawn.cs`：

```csharp
namespace Quoridor.Domain.Core;

public sealed record Pawn(PlayerId Owner, Cell Pos);
```

`src/Quoridor.Domain/Core/PlayerState.cs`：

```csharp
namespace Quoridor.Domain.Core;

public sealed record PlayerState(PlayerId Id, Cell Start, GoalEdge Goal, int WallsLeft);
```

`src/Quoridor.Domain/Core/GameState.cs`：

```csharp
using System.Collections.Immutable;
using System.Linq;

namespace Quoridor.Domain.Core;

public sealed record GameState(
    BoardConfig Config,
    ImmutableArray<PlayerState> Players,
    ImmutableArray<Pawn> Pawns,
    ImmutableArray<WallPos> Walls,
    PlayerId ActivePlayer,
    Phase Phase,
    PlayerId? Winner)
{
    public Pawn PawnOf(PlayerId id) => Pawns.Single(p => p.Owner == id);
    public PlayerState PlayerOf(PlayerId id) => Players.Single(p => p.Id == id);
    public bool IsFinished => Phase == Phase.Finished;
}
```

`src/Quoridor.Domain/Core/ImmutableArrayExtensions.cs`：

```csharp
using System.Collections.Immutable;

namespace Quoridor.Domain.Core;

public static class ImmutableArrayExtensions
{
    public static ImmutableArray<T> Replace<T>(this ImmutableArray<T> arr, T old, T @new)
        where T : class
    {
        var b = ImmutableArray.CreateBuilder<T>(arr.Length);
        bool replaced = false;
        foreach (var x in arr)
        {
            if (!replaced && x.Equals(old)) { b.Add(@new); replaced = true; }
            else b.Add(x);
        }
        return b.MoveToImmutable();
    }
}
```

`src/Quoridor.Domain/Core/GoalChecker.cs`：

```csharp
namespace Quoridor.Domain.Core;

public static class GoalChecker
{
    public static bool Reached(GoalEdge goal, Cell pos, BoardConfig cfg) => goal switch
    {
        GoalEdge.North => pos.Row == cfg.MaxIndex,
        GoalEdge.South => pos.Row == 0,
        GoalEdge.West => pos.Col == 0,
        GoalEdge.East => pos.Col == cfg.MaxIndex,
        _ => false,
    };
}
```

`src/Quoridor.Domain/Core/GameSetup.cs`：

```csharp
using System.Collections.Immutable;
using System.Linq;

namespace Quoridor.Domain.Core;

public static class GameSetup
{
    public static GameState CreateStandard2P() => Create(BoardConfig.Standard, 2);
    public static GameState CreateStandard4P() => Create(BoardConfig.Standard, 4);
    public static GameState CreateKid2P() => Create(BoardConfig.Kid, 2);
    public static GameState CreateKid4P() => Create(BoardConfig.Kid, 4);

    public static GameState Create(BoardConfig cfg, int players)
    {
        int mid = cfg.MaxIndex / 2;
        (Cell Start, GoalEdge Goal)[] defs = players switch
        {
            2 => new[] {
                (new Cell(mid, 0), GoalEdge.North),
                (new Cell(mid, cfg.MaxIndex), GoalEdge.South),
            },
            4 => new[] {
                (new Cell(mid, 0), GoalEdge.North),
                (new Cell(0, mid), GoalEdge.East),
                (new Cell(mid, cfg.MaxIndex), GoalEdge.South),
                (new Cell(cfg.MaxIndex, mid), GoalEdge.West),
            },
            _ => throw new System.ArgumentOutOfRangeException(nameof(players)),
        };
        int walls = WallBudget.PerPlayer(cfg.Variant, players);
        var ps = defs
            .Select((d, i) => new PlayerState((PlayerId)i, d.Start, d.Goal, walls))
            .ToImmutableArray();
        var pawns = ps.Select(p => new Pawn(p.Id, p.Start)).ToImmutableArray();
        return new GameState(cfg, ps, pawns, ImmutableArray<WallPos>.Empty,
            PlayerId.P1, Phase.Running, null);
    }
}
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test`
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(domain): 添加 GameState/PlayerState/Pawn/GameSetup/GoalChecker"
```

---

## Task 4: Path 层 — Directions / BoardGraph

**Files:**
- Create: `src/Quoridor.Domain/Path/Directions.cs`
- Create: `src/Quoridor.Domain/Path/BoardGraph.cs`
- Test: `tests/Quoridor.Domain.Tests/Path/BoardGraphTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Domain.Tests/Path/BoardGraphTests.cs`：

```csharp
using Quoridor.Domain.Core;
using Quoridor.Domain.Path;
using Xunit;

namespace Quoridor.Domain.Tests.Path;

public class BoardGraphTests
{
    [Fact]
    public void Empty_board_has_no_wall_between_adjacent_cells()
    {
        var g = new BoardGraph(GameSetup.CreateStandard2P());
        Assert.False(g.HasWallBetween(new Cell(4, 0), new Cell(4, 1)));
        Assert.True(g.InBounds(new Cell(0, 0)));
        Assert.True(g.InBounds(new Cell(8, 8)));
        Assert.False(g.InBounds(new Cell(9, 0)));
    }

    [Fact]
    public void Horizontal_wall_blocks_vertical_passages()
    {
        // 水平墙 anchor(4,3)：阻断 (4,3)-(4,4) 与 (5,3)-(5,4)
        var s = GameSetup.CreateStandard2P() with
        {
            Walls = System.Collections.Immutable.ImmutableArray.Create(new WallPos(new Cell(4, 3), WallOrient.Horizontal)),
        };
        var g = new BoardGraph(s);
        Assert.True(g.HasWallBetween(new Cell(4, 3), new Cell(4, 4)));
        Assert.True(g.HasWallBetween(new Cell(5, 3), new Cell(5, 4)));
        Assert.False(g.HasWallBetween(new Cell(4, 3), new Cell(3, 3)));
    }

    [Fact]
    public void Vertical_wall_blocks_horizontal_passages()
    {
        var s = GameSetup.CreateStandard2P() with
        {
            Walls = System.Collections.Immutable.ImmutableArray.Create(new WallPos(new Cell(4, 3), WallOrient.Vertical)),
        };
        var g = new BoardGraph(s);
        Assert.True(g.HasWallBetween(new Cell(4, 3), new Cell(5, 3)));
        Assert.True(g.HasWallBetween(new Cell(4, 4), new Cell(5, 4)));
        Assert.False(g.HasWallBetween(new Cell(4, 3), new Cell(4, 4)));
    }

    [Fact]
    public void EdgesOf_horizontal_returns_two_canonical_passages()
    {
        var edges = BoardGraph.EdgesOf(new WallPos(new Cell(4, 3), WallOrient.Horizontal));
        Assert.Contains(new Passage(new Cell(4, 3), new Cell(4, 4)), edges);
        Assert.Contains(new Passage(new Cell(5, 3), new Cell(5, 4)), edges);
    }
}
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test`
Expected: FAIL（类型未定义）。

- [ ] **Step 3: 写实现**

`src/Quoridor.Domain/Path/Directions.cs`：

```csharp
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
```

`src/Quoridor.Domain/Path/BoardGraph.cs`：

```csharp
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
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test`
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(domain): 添加 Directions 与 BoardGraph(无向图/墙边映射)"
```

---

## Task 5: PathFinder（BFS 最短路径）

**Files:**
- Create: `src/Quoridor.Domain/Path/PathFinder.cs`
- Test: `tests/Quoridor.Domain.Tests/Path/PathFinderTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Domain.Tests/Path/PathFinderTests.cs`：

```csharp
using Quoridor.Domain.Core;
using Quoridor.Domain.Path;
using Xunit;

namespace Quoridor.Domain.Tests.Path;

public class PathFinderTests
{
    [Fact]
    public void Empty_board_p1_to_north_distance_is_eight()
    {
        var s = GameSetup.CreateStandard2P();
        var g = new BoardGraph(s);
        var r = PathFinder.ShortestPath(g, s.PawnOf(PlayerId.P1).Pos, GoalEdge.North);
        Assert.Equal(8, r.Distance);
        Assert.Equal(9, r.Path.Length); // 起点 + 8 步
        Assert.Equal(new Cell(4, 8), r.Path[^1]);
    }

    [Fact]
    public void Horizontal_wall_in_front_increases_distance()
    {
        var s = GameSetup.CreateStandard2P() with
        {
            Walls = System.Collections.Immutable.ImmutableArray.Create(
                new WallPos(new Cell(4, 3), WallOrient.Horizontal)),
        };
        var g = new BoardGraph(s);
        var r = PathFinder.ShortestPath(g, new Cell(4, 0), GoalEdge.North);
        // 必须绕路，距离 > 8
        Assert.True(r.Distance > 8);
    }

    [Fact]
    public void Goal_at_start_returns_zero_distance()
    {
        var s = GameSetup.CreateStandard2P();
        var g = new BoardGraph(s);
        var r = PathFinder.ShortestPath(g, new Cell(4, 8), GoalEdge.North);
        Assert.Equal(0, r.Distance);
    }
}
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test`
Expected: FAIL。

- [ ] **Step 3: 写实现**

`src/Quoridor.Domain/Path/PathFinder.cs`：

```csharp
using System.Collections.Generic;
using System.Collections.Immutable;
using Quoridor.Domain.Core;

namespace Quoridor.Domain.Path;

public static class PathFinder
{
    public readonly record struct PathResult(int Distance, ImmutableArray<Cell> Path);

    public static PathResult ShortestPath(BoardGraph graph, Cell start, GoalEdge goal)
    {
        var cfg = graph.Config;
        var prev = new Dictionary<Cell, Cell>();
        var queue = new Queue<Cell>();
        queue.Enqueue(start);
        prev[start] = start;

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            if (GoalChecker.Reached(goal, cur, cfg))
                return BuildResult(prev, start, cur);

            foreach (var d in Directions.All)
            {
                var nb = Directions.Add(cur, d);
                if (!graph.InBounds(nb)) continue;
                if (graph.HasWallBetween(cur, nb)) continue;
                if (prev.ContainsKey(nb)) continue;
                prev[nb] = cur;
                queue.Enqueue(nb);
            }
        }
        return new PathResult(-1, ImmutableArray<Cell>.Empty);
    }

    private static PathResult BuildResult(Dictionary<Cell, Cell> prev, Cell start, Cell end)
    {
        var path = new List<Cell>();
        var c = end;
        int dist = 0;
        while (c != start) { path.Add(c); c = prev[c]; dist++; }
        path.Add(start);
        path.Reverse();
        return new PathResult(dist, path.ToImmutableArray());
    }
}
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test`
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(domain): 添加 PathFinder BFS 最短路径"
```

---

## Task 6: Reachability（设墙可达性校验）

**Files:**
- Create: `src/Quoridor.Domain/Path/Reachability.cs`
- Test: `tests/Quoridor.Domain.Tests/Path/ReachabilityTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Domain.Tests/Path/ReachabilityTests.cs`：

```csharp
using Quoridor.Domain.Core;
using Quoridor.Domain.Path;
using Xunit;

namespace Quoridor.Domain.Tests.Path;

public class ReachabilityTests
{
    [Fact]
    public void Empty_board_all_players_reach_goal()
    {
        Assert.True(Reachability.AllPlayersCanReachGoal(GameSetup.CreateStandard2P()));
        Assert.True(Reachability.AllPlayersCanReachGoal(GameSetup.CreateStandard4P()));
    }

    [Fact]
    public void Legal_partial_wall_keeps_reachability()
    {
        var s = GameSetup.CreateStandard2P() with
        {
            Walls = System.Collections.Immutable.ImmutableArray.Create(
                new WallPos(new Cell(4, 3), WallOrient.Horizontal)),
        };
        Assert.True(Reachability.AllPlayersCanReachGoal(s));
    }

    [Fact]
    public void Wall_sealing_p1_off_returns_false()
    {
        // 在 P1 起点行(row0)上方沿整行水平墙封死，堵死 P1 北上
        var walls = new System.Collections.Generic.List<WallPos>();
        for (int c = 0; c < 8; c++)
            walls.Add(new WallPos(new Cell(c, 0), WallOrient.Horizontal));
        var s = GameSetup.CreateStandard2P() with
        {
            Walls = walls.ToImmutableArray(),
        };
        Assert.False(Reachability.AllPlayersCanReachGoal(s));
    }
}
```

注意：`ToImmutableArray()` 需要 `using System.Collections.Immutable;` 与 `using System.Linq;`。在测试文件顶部补：

```csharp
using System.Collections.Immutable;
using System.Linq;
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test`
Expected: FAIL。

- [ ] **Step 3: 写实现**

`src/Quoridor.Domain/Path/Reachability.cs`：

```csharp
using Quoridor.Domain.Core;

namespace Quoridor.Domain.Path;

public static class Reachability
{
    public static bool AllPlayersCanReachGoal(GameState state)
    {
        var graph = new BoardGraph(state);
        foreach (var pawn in state.Pawns)
        {
            var goal = state.PlayerOf(pawn.Owner).Goal;
            if (PathFinder.ShortestPath(graph, pawn.Pos, goal).Distance < 0)
                return false;
        }
        return true;
    }
}
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test`
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(domain): 添加 Reachability 设墙可达性校验"
```

---

## Task 7: Commands / Events 类型

**Files:**
- Create: `src/Quoridor.Domain/Core/Commands.cs`
- Create: `src/Quoridor.Domain/Core/Events.cs`
- Test: `tests/Quoridor.Domain.Tests/Core/CommandsEventsTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Domain.Tests/Core/CommandsEventsTests.cs`：

```csharp
using Quoridor.Domain.Core;
using Xunit;

namespace Quoridor.Domain.Tests.Core;

public class CommandsEventsTests
{
    [Fact]
    public void Commands_are_distinct_types()
    {
        IGameCommand a = new MovePawnCommand(new Cell(4, 1));
        IGameCommand b = new PlaceWallCommand(new WallPos(new Cell(0, 0), WallOrient.Horizontal));
        Assert.IsType<MovePawnCommand>(a);
        Assert.IsType<PlaceWallCommand>(b);
    }

    [Fact]
    public void Events_carry_player_and_reason()
    {
        IGameEvent e = new WallRejected(PlayerId.P1, new WallPos(new Cell(0, 0), WallOrient.Horizontal), RejectReason.WallOverlap);
        var wr = Assert.IsType<WallRejected>(e);
        Assert.Equal(PlayerId.P1, wr.Who);
        Assert.Equal(RejectReason.WallOverlap, wr.Reason);
    }
}
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test`
Expected: FAIL。

- [ ] **Step 3: 写实现**

`src/Quoridor.Domain/Core/Commands.cs`：

```csharp
namespace Quoridor.Domain.Core;

public interface IGameCommand;
public sealed record MovePawnCommand(Cell To) : IGameCommand;
public sealed record PlaceWallCommand(WallPos Wall) : IGameCommand;
```

`src/Quoridor.Domain/Core/Events.cs`：

```csharp
namespace Quoridor.Domain.Core;

public interface IGameEvent;
public sealed record PawnMoved(PlayerId Who, Cell From, Cell To, MoveKind Kind) : IGameEvent;
public sealed record WallPlaced(PlayerId Who, WallPos Wall) : IGameEvent;
public sealed record WallRejected(PlayerId Who, WallPos Wall, RejectReason Reason) : IGameEvent;
public sealed record MoveRejected(PlayerId Who, Cell To, RejectReason Reason) : IGameEvent;
public sealed record PlayerWon(PlayerId Who) : IGameEvent;
public sealed record TurnPassed(PlayerId Next) : IGameEvent;
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test`
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(domain): 添加命令与事件类型"
```

---

## Task 8: MoveLegality（合法走子目标格集合：直走 / 直跳 / 斜跳）

**Files:**
- Create: `src/Quoridor.Domain/Rules/MoveLegality.cs`
- Test: `tests/Quoridor.Domain.Tests/Rules/MoveLegalityTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Domain.Tests/Rules/MoveLegalityTests.cs`：

```csharp
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Domain.Tests.Rules;

public class MoveLegalityTests
{
    [Fact]
    public void Empty_board_p1_can_step_to_four_neighbors()
    {
        var s = GameSetup.CreateStandard2P();
        var targets = MoveLegality.LegalTargets(s);
        Assert.Contains(new Cell(4, 1), targets);
        Assert.Contains(new Cell(3, 0), targets);
        Assert.Contains(new Cell(5, 0), targets);
        // row 0 在底边，不能向南出界
        Assert.DoesNotContain(new Cell(4, -1), targets);
    }

    [Fact]
    public void Straight_jump_over_adjacent_opponent()
    {
        // P1 在 (4,0)，把 P2 放到 (4,1) 相邻，无墙 → 可直跳到 (4,2)
        var s = PlaceP2At(GameSetup.CreateStandard2P(), new Cell(4, 1));
        var targets = MoveLegality.LegalTargets(s);
        Assert.Contains(new Cell(4, 2), targets);
    }

    [Fact]
    public void Jump_blocked_by_wall_behind_opponent_allows_diagonal()
    {
        // P1(4,0) 对 P2(4,1)；身后(4,1)-(4,2) 有水平墙 → 直跳被挡，允许斜跳到 (3,1)/(5,1)
        var s = PlaceP2At(GameSetup.CreateStandard2P(), new Cell(4, 1));
        s = s with
        {
            Walls = System.Collections.Immutable.ImmutableArray.Create(
                new WallPos(new Cell(4, 1), WallOrient.Horizontal)),
        };
        var targets = MoveLegality.LegalTargets(s);
        Assert.DoesNotContain(new Cell(4, 2), targets); // 直跳被封
        Assert.Contains(new Cell(3, 1), targets);
        Assert.Contains(new Cell(5, 1), targets);
    }

    [Fact]
    public void Step_blocked_by_wall_is_excluded()
    {
        var s = GameSetup.CreateStandard2P() with
        {
            Walls = System.Collections.Immutable.ImmutableArray.Create(
                new WallPos(new Cell(4, 0), WallOrient.Horizontal)),
        };
        var targets = MoveLegality.LegalTargets(s);
        Assert.DoesNotContain(new Cell(4, 1), targets);
    }

    private static GameState PlaceP2At(GameState s, Cell c) =>
        s with { Pawns = s.Pawns.Replace(s.PawnOf(PlayerId.P2), s.PawnOf(PlayerId.P2) with { Pos = c }) };
}
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test`
Expected: FAIL。

- [ ] **Step 3: 写实现**

`src/Quoridor.Domain/Rules/MoveLegality.cs`：

```csharp
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Quoridor.Domain.Core;
using Quoridor.Domain.Path;

namespace Quoridor.Domain.Rules;

public static class MoveLegality
{
    public static ImmutableArray<Cell> LegalTargets(GameState state)
    {
        var graph = new BoardGraph(state);
        var pawn = state.PawnOf(state.ActivePlayer);
        var result = new List<Cell>();

        foreach (var d in Directions.All)
        {
            var nb = Directions.Add(pawn.Pos, d);
            if (!graph.InBounds(nb)) continue;
            if (graph.HasWallBetween(pawn.Pos, nb)) continue;

            if (OccupantAt(state, nb) is null)
            {
                result.Add(nb); // 普通一步
            }
            else
            {
                var beyond = Directions.Add(nb, d);
                bool canStraight = graph.InBounds(beyond)
                    && !graph.HasWallBetween(nb, beyond)
                    && OccupantAt(state, beyond) is null;
                if (canStraight)
                {
                    result.Add(beyond); // 直跳
                }
                else
                {
                    foreach (var perp in Directions.Perpendiculars(d))
                    {
                        var side = Directions.Add(nb, perp);
                        if (!graph.InBounds(side)) continue;
                        if (graph.HasWallBetween(nb, side)) continue;
                        if (OccupantAt(state, side) is not null) continue;
                        result.Add(side); // 斜跳
                    }
                }
            }
        }
        return result.Distinct().ToImmutableArray();
    }

    private static Pawn? OccupantAt(GameState state, Cell c)
    {
        foreach (var p in state.Pawns)
            if (p.Pos == c) return p;
        return null;
    }
}
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test`
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(domain): 添加 MoveLegality 走子合法性(含直跳/斜跳)"
```

---

## Task 9: WallLegality（越界 / 重叠 / 可达性）

**Files:**
- Create: `src/Quoridor.Domain/Rules/WallLegality.cs`
- Test: `tests/Quoridor.Domain.Tests/Rules/WallLegalityTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Domain.Tests/Rules/WallLegalityTests.cs`：

```csharp
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Domain.Tests.Rules;

public class WallLegalityTests
{
    [Fact]
    public void In_bounds_legal_wall_returns_null()
    {
        var s = GameSetup.CreateStandard2P();
        var w = new WallPos(new Cell(0, 0), WallOrient.Horizontal);
        Assert.Null(WallLegality.Validate(s, w));
    }

    [Fact]
    public void Out_of_bounds_anchor_rejected()
    {
        var s = GameSetup.CreateStandard2P();
        // MaxIndex=8，合法 anchor 上限 7
        Assert.Equal(RejectReason.WallOutOfBounds,
            WallLegality.Validate(s, new WallPos(new Cell(8, 0), WallOrient.Horizontal)));
        Assert.Equal(RejectReason.WallOutOfBounds,
            WallLegality.Validate(s, new WallPos(new Cell(0, 8), WallOrient.Vertical)));
    }

    [Fact]
    public void Overlapping_same_slot_rejected_but_crossing_orientation_legal()
    {
        var s = GameSetup.CreateStandard2P() with
        {
            Walls = System.Collections.Immutable.ImmutableArray.Create(
                new WallPos(new Cell(4, 3), WallOrient.Horizontal)),
        };
        // 同位重复（同 anchor 同朝向）：共享两条边 → 重叠
        Assert.Equal(RejectReason.WallOverlap,
            WallLegality.Validate(s, new WallPos(new Cell(4, 3), WallOrient.Horizontal)));
        // 不同朝向同 anchor：H 阻竖直通道、V 阻水平通道，无共享边，仅交于一点 → 合法（墙可交叉）
        Assert.Null(WallLegality.Validate(s, new WallPos(new Cell(4, 3), WallOrient.Vertical)));
    }

    [Fact]
    public void Wall_that_seals_player_rejected()
    {
        // P1 置于左下角 (0,0)；已放 V(0,0) 阻断东出口（北出口仍开，P1 可达 row8，故该墙合法）。
        var initial = GameSetup.CreateStandard2P();
        var p1 = initial.PawnOf(PlayerId.P1);
        var sealedState = initial with
        {
            Pawns = initial.Pawns.Replace(p1, p1 with { Pos = new Cell(0, 0) }),
            Walls = System.Collections.Immutable.ImmutableArray.Create(
                new WallPos(new Cell(0, 0), WallOrient.Vertical)),
        };
        // 再放 H(0,0) 封死北出口：P1 东(V)北(H)皆堵，南西为棋盘边界 → 无路可达 row8
        // → WallBlocksAllPaths（H 与 V 无共享边，不触发重叠，直接到可达性校验失败）
        var reason = WallLegality.Validate(sealedState, new WallPos(new Cell(0, 0), WallOrient.Horizontal));
        Assert.Equal(RejectReason.WallBlocksAllPaths, reason);
    }
}
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test`
Expected: FAIL。

- [ ] **Step 3: 写实现**

`src/Quoridor.Domain/Rules/WallLegality.cs`：

```csharp
using System.Collections.Generic;
using System.Linq;
using Quoridor.Domain.Core;
using Quoridor.Domain.Path;

namespace Quoridor.Domain.Rules;

public static class WallLegality
{
    public static RejectReason? Validate(GameState state, WallPos wall)
    {
        var cfg = state.Config;
        if (wall.Anchor.Col < 0 || wall.Anchor.Row < 0
            || wall.Anchor.Col >= cfg.MaxIndex || wall.Anchor.Row >= cfg.MaxIndex)
        {
            return RejectReason.WallOutOfBounds;
        }

        var newEdges = BoardGraph.EdgesOf(wall).ToHashSet();
        foreach (var existing in state.Walls)
            foreach (var e in BoardGraph.EdgesOf(existing))
                if (newEdges.Contains(e))
                    return RejectReason.WallOverlap;

        var tentative = state with { Walls = state.Walls.Add(wall) };
        if (!Reachability.AllPlayersCanReachGoal(tentative))
            return RejectReason.WallBlocksAllPaths;

        return null;
    }
}
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test`
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(domain): 添加 WallLegality 墙合法性(越界/重叠/可达性)"
```

---

## Task 10: RuleEngine.ValidateAndApply（总入口：走子 / 设墙 / 回合 / 胜负）

**Files:**
- Create: `src/Quoridor.Domain/Rules/RuleEngine.cs`
- Test: `tests/Quoridor.Domain.Tests/Rules/RuleEngineTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Domain.Tests/Rules/RuleEngineTests.cs`：

```csharp
using System.Linq;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Domain.Tests.Rules;

public class RuleEngineTests
{
    [Fact]
    public void Legal_step_advances_turn()
    {
        var s = GameSetup.CreateStandard2P();
        var r = RuleEngine.ValidateAndApply(s, new MovePawnCommand(new Cell(4, 1)));
        Assert.NotNull(r.State);
        Assert.Equal(new Cell(4, 1), r.State!.PawnOf(PlayerId.P1).Pos);
        Assert.Equal(PlayerId.P2, r.State.ActivePlayer);
        Assert.Contains(r.Events, e => e is PawnMoved);
        Assert.Contains(r.Events, e => e is TurnPassed);
    }

    [Fact]
    public void Illegal_step_is_rejected_state_unchanged()
    {
        var s = GameSetup.CreateStandard2P();
        var r = RuleEngine.ValidateAndApply(s, new MovePawnCommand(new Cell(4, 2))); // 跨两格非法
        Assert.Null(r.State);
        Assert.Contains(r.Events, e => e is MoveRejected);
    }

    [Fact]
    public void Legal_wall_consumes_one_wall_and_passes_turn()
    {
        var s = GameSetup.CreateStandard2P();
        var r = RuleEngine.ValidateAndApply(s, new PlaceWallCommand(new WallPos(new Cell(0, 0), WallOrient.Horizontal)));
        Assert.NotNull(r.State);
        Assert.Equal(9, r.State!.PlayerOf(PlayerId.P1).WallsLeft);
        Assert.Equal(PlayerId.P2, r.State.ActivePlayer);
        Assert.Contains(r.Events, e => e is WallPlaced);
    }

    [Fact]
    public void Wall_when_none_left_rejected()
    {
        var s = GameSetup.CreateStandard2P();
        // 手动把 P1 墙数置 0
        s = s with
        {
            Players = s.Players.Replace(s.PlayerOf(PlayerId.P1),
                s.PlayerOf(PlayerId.P1) with { WallsLeft = 0 }),
        };
        var r = RuleEngine.ValidateAndApply(s, new PlaceWallCommand(new WallPos(new Cell(0, 0), WallOrient.Horizontal)));
        Assert.Null(r.State);
        Assert.Contains(r.Events, e => e is WallRejected { Reason: RejectReason.NoWallsLeft });
    }

    [Fact]
    public void Reaching_goal_ends_game_with_win()
    {
        // P1 放到 (4,7)，北边目标 row8，一步即胜；把 P2 从 (4,8) 挪到 (3,8) 让出目标格
        var s = GameSetup.CreateStandard2P();
        s = s with { Pawns = s.Pawns.Replace(s.PawnOf(PlayerId.P1), s.PawnOf(PlayerId.P1) with { Pos = new Cell(4, 7) }) };
        s = s with { Pawns = s.Pawns.Replace(s.PawnOf(PlayerId.P2), s.PawnOf(PlayerId.P2) with { Pos = new Cell(3, 8) }) };
        var r = RuleEngine.ValidateAndApply(s, new MovePawnCommand(new Cell(4, 8)));
        Assert.NotNull(r.State);
        Assert.Equal(Phase.Finished, r.State!.Phase);
        Assert.Equal(PlayerId.P1, r.State.Winner);
        Assert.Contains(r.Events, e => e is PlayerWon);
        Assert.DoesNotContain(r.Events, e => e is TurnPassed);
    }

    [Fact]
    public void Move_after_finished_rejected()
    {
        var finished = GameSetup.CreateStandard2P() with { Phase = Phase.Finished, Winner = PlayerId.P1 };
        var r = RuleEngine.ValidateAndApply(finished, new MovePawnCommand(new Cell(4, 1)));
        Assert.Null(r.State);
        Assert.Contains(r.Events, e => e is MoveRejected { Reason: RejectReason.GameFinished });
    }
}
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test`
Expected: FAIL。

- [ ] **Step 3: 写实现**

`src/Quoridor.Domain/Rules/RuleEngine.cs`：

```csharp
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Quoridor.Domain.Core;
using Quoridor.Domain.Path;

namespace Quoridor.Domain.Rules;

public static class RuleEngine
{
    public readonly record struct ApplyResult(GameState? State, ImmutableArray<IGameEvent> Events);

    public static ApplyResult ValidateAndApply(GameState state, IGameCommand command)
    {
        if (state.IsFinished)
        {
            return command switch
            {
                MovePawnCommand m => RejectMove(state.ActivePlayer, m.To, RejectReason.GameFinished),
                PlaceWallCommand w => RejectWall(state.ActivePlayer, w.Wall, RejectReason.GameFinished),
                _ => throw new ArgumentOutOfRangeException(nameof(command)),
            };
        }
        return command switch
        {
            MovePawnCommand m => ApplyMove(state, state.ActivePlayer, m),
            PlaceWallCommand w => ApplyWall(state, state.ActivePlayer, w),
            _ => throw new ArgumentOutOfRangeException(nameof(command)),
        };
    }

    private static ApplyResult ApplyMove(GameState state, PlayerId who, MovePawnCommand cmd)
    {
        var targets = MoveLegality.LegalTargets(state);
        if (!targets.Contains(cmd.To))
            return RejectMove(who, cmd.To, RejectReason.IllegalMove);

        var pawn = state.PawnOf(who);
        var kind = Classify(pawn.Pos, cmd.To);
        var newPawns = state.Pawns.Replace(pawn, pawn with { Pos = cmd.To });
        var events = new List<IGameEvent> { new PawnMoved(who, pawn.Pos, cmd.To, kind) };

        GameState next = state with { Pawns = newPawns };
        if (GoalChecker.Reached(state.PlayerOf(who).Goal, cmd.To, state.Config))
        {
            next = next with { Phase = Phase.Finished, Winner = who };
            events.Add(new PlayerWon(who));
        }
        else
        {
            var nx = NextPlayer(state);
            next = next with { ActivePlayer = nx };
            events.Add(new TurnPassed(nx));
        }
        return new ApplyResult(next, events.ToImmutableArray());
    }

    private static ApplyResult ApplyWall(GameState state, PlayerId who, PlaceWallCommand cmd)
    {
        var player = state.PlayerOf(who);
        if (player.WallsLeft <= 0)
            return RejectWall(who, cmd.Wall, RejectReason.NoWallsLeft);

        var reason = WallLegality.Validate(state, cmd.Wall);
        if (reason is not null)
            return RejectWall(who, cmd.Wall, reason.Value);

        var newPlayers = state.Players.Replace(player, player with { WallsLeft = player.WallsLeft - 1 });
        var nx = NextPlayer(state);
        var next = state with { Players = newPlayers, Walls = state.Walls.Add(cmd.Wall), ActivePlayer = nx };
        var events = new List<IGameEvent> { new WallPlaced(who, cmd.Wall), new TurnPassed(nx) };
        return new ApplyResult(next, events.ToImmutableArray());
    }

    private static PlayerId NextPlayer(GameState state)
    {
        int n = state.Players.Length;
        return (PlayerId)(((int)state.ActivePlayer + 1) % n);
    }

    private static MoveKind Classify(Cell from, Cell to)
    {
        int dc = Math.Abs(to.Col - from.Col);
        int dr = Math.Abs(to.Row - from.Row);
        if (dc + dr == 1) return MoveKind.Step;
        if ((dc == 2 && dr == 0) || (dr == 2 && dc == 0)) return MoveKind.Jump;
        return MoveKind.DiagonalJump;
    }

    private static ApplyResult RejectMove(PlayerId who, Cell to, RejectReason r) =>
        new(null, ImmutableArray.Create<IGameEvent>(new MoveRejected(who, to, r)));

    private static ApplyResult RejectWall(PlayerId who, WallPos w, RejectReason r) =>
        new(null, ImmutableArray.Create<IGameEvent>(new WallRejected(who, w, r)));
}
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test`
Expected: PASS（全部）。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(domain): 添加 RuleEngine.ValidateAndApply 总入口(走子/设墙/回合/胜负)"
```

---

## Task 11: NotationService.Encode

**Files:**
- Create: `src/Quoridor.Domain/Notation/NotationService.cs`
- Test: `tests/Quoridor.Domain.Tests/Notation/NotationServiceTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Domain.Tests/Notation/NotationServiceTests.cs`：

```csharp
using Quoridor.Domain.Core;
using Quoridor.Domain.Notation;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Domain.Tests.Notation;

public class NotationServiceTests
{
    [Fact]
    public void Cell_and_wall_to_notation()
    {
        Assert.Equal("e2", NotationService.CellToNotation(new Cell(4, 1)));
        Assert.Equal("e3v", NotationService.WallToNotation(new WallPos(new Cell(4, 2), WallOrient.Vertical)));
        Assert.Equal("a1", NotationService.CellToNotation(new Cell(0, 0)));
        Assert.Equal("i9", NotationService.CellToNotation(new Cell(8, 8)));
    }

    [Fact]
    public void Encode_two_player_sequence()
    {
        // P1: e1->e2 ; P2: e9->e8 ; P1: e2->e3 ; P2: e8->e7
        var s = GameSetup.CreateStandard2P();
        var events = new System.Collections.Generic.List<IGameEvent>();
        s = Apply(s, new MovePawnCommand(new Cell(4, 1)), events);
        s = Apply(s, new MovePawnCommand(new Cell(4, 7)), events);
        s = Apply(s, new MovePawnCommand(new Cell(4, 2)), events);
        s = Apply(s, new MovePawnCommand(new Cell(4, 6)), events);

        Assert.Equal("1. e2 e8 2. e3 e7", NotationService.Encode(events, 2));
    }

    [Fact]
    public void Encode_four_player_sequence()
    {
        var s = GameSetup.CreateStandard4P();
        var events = new System.Collections.Generic.List<IGameEvent>();
        s = Apply(s, new MovePawnCommand(new Cell(4, 1)), events); // P1 e2
        s = Apply(s, new MovePawnCommand(new Cell(1, 4)), events); // P2 b5
        s = Apply(s, new MovePawnCommand(new Cell(4, 7)), events); // P3 e8
        s = Apply(s, new MovePawnCommand(new Cell(7, 4)), events); // P4 h5

        Assert.Equal("1. e2 b5 e8 h5", NotationService.Encode(events, 4));
    }

    [Fact]
    public void Encode_includes_wall_ply()
    {
        var s = GameSetup.CreateStandard2P();
        var events = new System.Collections.Generic.List<IGameEvent>();
        s = Apply(s, new PlaceWallCommand(new WallPos(new Cell(4, 2), WallOrient.Vertical)), events); // e3v
        s = Apply(s, new MovePawnCommand(new Cell(4, 7)), events); // P2 e8

        Assert.Equal("1. e3v e8", NotationService.Encode(events, 2));
    }

    private static GameState Apply(GameState s, IGameCommand c, System.Collections.Generic.List<IGameEvent> ev)
    {
        var r = RuleEngine.ValidateAndApply(s, c);
        Assert.NotNull(r.State);
        ev.AddRange(r.Events);
        return r.State!;
    }
}
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test`
Expected: FAIL。

- [ ] **Step 3: 写实现**

`src/Quoridor.Domain/Notation/NotationService.cs`：

```csharp
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
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test`
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(domain): 添加 NotationService.Encode 记谱编码"
```

---

## Task 12: NotationService.Decode 与 Replay（往返闭环）

**Files:**
- Modify: `src/Quoridor.Domain/Notation/NotationService.cs`（追加 Decode / Replay / ParseCell / ParseError）
- Test: `tests/Quoridor.Domain.Tests/Notation/NotationDecodeTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Domain.Tests/Notation/NotationDecodeTests.cs`：

```csharp
using Quoridor.Domain.Core;
using Quoridor.Domain.Notation;
using Xunit;

namespace Quoridor.Domain.Tests.Notation;

public class NotationDecodeTests
{
    [Fact]
    public void Decode_parses_moves_and_walls()
    {
        var cmds = NotationService.Decode("1. e2 e3v e8");
        Assert.Equal(3, cmds.Length);
        Assert.Equal(new Cell(4, 1), ((MovePawnCommand)cmds[0]).To);
        Assert.Equal(new WallPos(new Cell(4, 2), WallOrient.Vertical), ((PlaceWallCommand)cmds[1]).Wall);
        Assert.Equal(new Cell(4, 7), ((MovePawnCommand)cmds[2]).To);
    }

    [Fact]
    public void Decode_handles_inline_and_spaced_round_markers()
    {
        var a = NotationService.Decode("1.e2 1.e8");
        Assert.Equal(2, a.Length);
        var b = NotationService.Decode("1. e2 e8 2. e3 e7");
        Assert.Equal(4, b.Length);
    }

    [Fact]
    public void Replay_roundtrips_two_player_game()
    {
        var notation = "1. e2 e8 2. e3 e7";
        var final = NotationService.Replay(BoardConfig.Standard, 2, notation);
        Assert.Equal(new Cell(4, 2), final.PawnOf(PlayerId.P1).Pos);
        Assert.Equal(new Cell(4, 6), final.PawnOf(PlayerId.P2).Pos);
        Assert.Equal(PlayerId.P1, final.ActivePlayer);
    }

    [Fact]
    public void Decode_invalid_token_throws_parse_error()
    {
        // 非法 token "z9" 列越界（z 远超棋盘），Replay 时由规则拒绝
        Assert.Throws<NotationParseException>(() =>
            NotationService.Replay(BoardConfig.Standard, 2, "1. z9 e8"));
    }
}
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test`
Expected: FAIL。

- [ ] **Step 3: 写实现（追加到 NotationService.cs）**

在 `NotationService` 类内追加（保留已有 `Encode` 等成员）：

```csharp
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
            var r = Quoridor.Domain.Rules.RuleEngine.ValidateAndApply(state, cmd);
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
```

在文件顶部追加 `using`（如尚无）：

```csharp
using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Quoridor.Domain.Core;
```

注意：`ToImmutableArray()` 需要 `System.Collections.Immutable` 与 `System.Linq`。

并在 `Notation` 命名空间内（同文件或新文件 `NotationParseException.cs`）追加异常类型：

`src/Quoridor.Domain/Notation/NotationParseException.cs`：

```csharp
using System;

namespace Quoridor.Domain.Notation;

public sealed class NotationParseException : Exception
{
    public NotationParseException(string message) : base(message) { }
}
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test`
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(domain): 添加 NotationService.Decode/Replay 与解析异常"
```

---

## Task 13: 集成回归用例与 README

**Files:**
- Create: `tests/Quoridor.Domain.Tests/Cases/FullGameCases.cs`
- Create: `src/Quoridor.Domain/README.md`

- [ ] **Step 1: 写失败测试（一个完整 2 人对局 + 一个 4 人首回合）**

`tests/Quoridor.Domain.Tests/Cases/FullGameCases.cs`：

```csharp
using Quoridor.Domain.Core;
using Quoridor.Domain.Notation;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Domain.Tests.Cases;

public class FullGameCases
{
    [Fact]
    public void Two_player_sprint_to_win_round_trips()
    {
        // P1 从 (4,0) 北上直冲到 (4,8) 获胜；P2 在顶行 (3,8)<->(4,8) 往复让出冲刺道。
        // i=0..6：P1 走到 (4,1)..(4,7)，与顶行的 P2 不相邻；i=7：P1 走到 (4,8) 时 P2 恰在 (3,8)，(4,8) 空出且无墙 → 落子并触发 PlayerWon，循环 break。
        var s = GameSetup.CreateStandard2P();
        var events = new System.Collections.Generic.List<IGameEvent>();
        var p2Cells = new Cell[]
        {
            new(3, 8), new(4, 8), new(3, 8), new(4, 8),
            new(3, 8), new(4, 8), new(3, 8), new(4, 8),
        };
        for (int i = 0; i < 8; i++)
        {
            s = Step(s, new Cell(4, i + 1), events);   // P1 北上
            if (s.IsFinished) break;                   // P1 到顶，结束
            s = Step(s, p2Cells[i], events);           // P2 顶行往复
        }
        Assert.Equal(Phase.Finished, s.Phase);
        Assert.Equal(PlayerId.P1, s.Winner);
        Assert.Equal(new Cell(4, 8), s.PawnOf(PlayerId.P1).Pos);

        // 记谱往返一致
        var notation = NotationService.Encode(events, 2);
        var replayed = NotationService.Replay(BoardConfig.Standard, 2, notation);
        Assert.Equal(s.PawnOf(PlayerId.P1).Pos, replayed.PawnOf(PlayerId.P1).Pos);
        Assert.Equal(s.Winner, replayed.Winner);
    }

    [Fact]
    public void Four_player_first_round_four_distinct_actors()
    {
        var s = GameSetup.CreateStandard4P();
        var events = new System.Collections.Generic.List<IGameEvent>();
        s = Step(s, new Cell(4, 1), events);
        s = Step(s, new Cell(1, 4), events);
        s = Step(s, new Cell(4, 7), events);
        s = Step(s, new Cell(7, 4), events);
        Assert.Equal("1. e2 b5 e8 h5", NotationService.Encode(events, 4));
    }

    private static GameState Step(GameState s, Cell to, System.Collections.Generic.List<IGameEvent> ev)
    {
        var r = RuleEngine.ValidateAndApply(s, new MovePawnCommand(to));
        Assert.NotNull(r.State);
        ev.AddRange(r.Events);
        return r.State!;
    }
}
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test`
Expected: FAIL（`FullGameCases` 类尚未存在 / 编译错误）。

- [ ] **Step 3: 运行并验证通过**

Run: `dotnet test`
Expected: PASS（全部用例）。走法可走通性已推演：i=0..6 时 P1 在 (4,i+1) 而 P2 在顶行 (3,8)/(4,8)，二者不相邻；i=7 时 P1 落到 (4,8)、P2 恰在 (3,8)，(4,8) 空出且无墙 → 合法并触发 `PlayerWon`，循环 `break`。若个别用例失败，先核对本推演再修正测试，**不改被测生产代码以迎合测试**。

- [ ] **Step 4: 写 README**

`src/Quoridor.Domain/README.md`：

```markdown
# Quoridor.Domain

Quoridor 游戏 domain 核心类库（零 Godot 依赖）。

## 构建

```
dotnet build
```

## 测试

```
dotnet test
```

## 职责

- `Core`：不可变数据模型、命令、事件、初始局面工厂。
- `Path`：无向图 `BoardGraph`、BFS `PathFinder`、`Reachability`。
- `Rules`：`MoveLegality` / `WallLegality` / `RuleEngine.ValidateAndApply`。
- `Notation`：Modern Algebraic Notation 编/解码与回放。

## 唯一状态变更入口

```
ApplyResult r = RuleEngine.ValidateAndApply(state, command);
// r.State 非空 -> 已应用；为空 -> 被拒绝，state 不变，r.Events 含 Rejected。
```

后续 AI、Application、UI 在其上叠加，不修改本库内部状态结构。
```

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "test(domain): 添加完整对局回归用例与 README"
```

---

## Self-Review（计划自检结果）

**1. Spec 覆盖（对应 spec 各节）：**
- §2 规则 → Task 8/9/10（走子/跳子、设墙、可达性、胜负）覆盖。
- §4 数据模型/命令/事件 → Task 2/3/7 覆盖。
- §5 规则引擎与路径算法 → Task 4/5/6（图/BFS/可达性）覆盖。
- §2.5 记谱 → Task 11/12（Encode/Decode/Replay）覆盖。
- §8 测试 → 各任务 TDD + Task 13 回归用例覆盖。
- 未覆盖：AI（spec §5.4）——属 Plan 2；Application/UI——属 Plan 3/4。本计划范围明确不含，符合分层拆分。

**2. 占位符扫描：** 无 TBD/TODO；Task 13 Step 2 的对局修正给了具体 `p2Cells` 数组与可执行调整指引，非空泛"处理边界"。

**3. 类型一致性：** `ApplyResult`、`ValidateAndApply`、`LegalTargets`、`WallLegality.Validate`、`NotationService.Encode/Decode/Replay`、`NotationParseException` 在定义与后续引用处签名一致；`Replace<T>` 约束为 `class`（Pawn/PlayerState 为 sealed record class，匹配）；`Cell`/`WallPos`/`Passage` 用法一致。

**已修正：** Task 6 测试需补 `using System.Collections.Immutable;` `using System.Linq;`——已在步骤内注明。

---

## 执行交接

计划已保存到 `docs/superpowers/plans/2026-06-30-quoridor-domain-core.md`。两种执行方式：

1. **Subagent-Driven（推荐）** —— 每个任务派发独立子 agent 实现，任务间两阶段评审，迭代快。
2. **Inline Execution** —— 在当前会话用 executing-plans 批量执行，带检查点。

选哪种？
