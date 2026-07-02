# Quoridor Application 实现计划（Plan 3）

> **状态：✅ 已完成并合入 master（commit 9b988e5，103 测试通过：Domain 68 + Application 35）。** 9 个任务全部 TDD 实现并经逐任务 spec + 代码质量双评审 + 最终整体评审。Plan 3 实现中浮现的 Plan 4 结转项见项目记忆 `plan2-4-carryover`。

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 新增 `Quoridor.Application` 纯 C# 类库：`GameSession`（命令调度 + 事件广播 + AI 自动驱动 + 日志）、`PreviewService`（设墙悬浮预览）、`ReplayController`（记谱导出/导入/步进回放）、`IPlayer` 座位抽象 + `AiPlayer` 适配器（难度→AI 映射）、轻量 `IAppLogger`；并清 Plan 1/2 遗留技术债（NotationService 范围校验+续谱占位、AI 入口守卫、难度映射、脚手架清理、断言增强）。

**Architecture:** Application 为独立类库 `Quoridor.Application`（net10.0，引用 `Quoridor.Domain`），不引入 Godot 或新外部依赖。`GameSession` 持有当前 `GameState` + 座位表，人手与 AI 走同一 `Submit → RuleEngine.ValidateAndApply → 广播` 通道；AI 座位由 `GameSession` 在人手之后**自动驱动**（循环到人类回合或终局）。事件用简单 `event Action<IGameEvent>` 广播给订阅者。日志用自建 `IAppLogger`（默认 `NullAppLogger`，零依赖，Plan 4 Godot 注入真实实现）。Domain 不改日志，Application 在 `Submit`/`DriveAi` 入口出口、规则拒绝、AI 决策处落日志。

**Tech Stack:** C# 14 / .NET 10 / xUnit / `System.Collections.Immutable`。无新 NuGet 包。

**所属设计：** `docs/superpowers/specs/2026-06-30-quoridor-design.md` §3（架构）、§6（数据流）、§7（错误处理）。**前置：** Plan 1（Domain Core）+ Plan 2（AI）已合入 master，63 测试通过。

**设计决策（已与用户确认）：**

1. **事件广播**：简单 `event Action<IGameEvent>? EventOccurred`（零依赖，够 Plan 4 Godot 订阅）。
2. **日志**：自建轻量 `IAppLogger`（`Log(LogLevel, string, params object[])` + `NullAppLogger`），不引入 `Microsoft.Extensions.Logging`，与 Domain"零依赖可移植"风格一致。
3. **AI 驱动**：`GameSession` 自动驱动——`Submit` 处理完一手后，若下一活跃玩家是 AI 座位，自动调 `IQuoridorAi.Choose` 并 `Submit` 回环，循环到人类回合或终局。
4. **技术债范围**：纳入债1（ParseCell 范围校验）、债2（Decode 续谱占位）、债4（删脚手架）、债5（断言增强+封死测试注释）、债7（AI 入口守卫 IsFinished）、债9（难度→AI 映射）。**不纳入**债3（ImmutableArray.Replace 替换，可选，不动）、债6（MinimaxAi 性能，接入后实测再定）、债8（Medium 自对弈覆盖，本计划以"限步"集成测试自然补上）。

**偏离说明（相对 spec §3）：**

- spec §3 列 Application 含 `Presenter`。本计划不单建 `Presenter` 类——`GameSession` 即编排中枢（Submit+事件+AI 驱动），`PreviewService`/`ReplayController` 各司其职。Plan 4 Godot 直接订阅 `EventOccurred` + 调 `PreviewService`/`ReplayController`，无需多一层 facade（YAGNI）。
- spec §3 说"UI 仅依赖 Application 层接口，不直接碰 Domain 内部"。本计划让 `IGameCommand`/`IGameEvent`/`GameState` 等 Domain 类型经 Application 流通（GameSession 暴露 `GameState` 只读、`Submit(IGameCommand)`、`EventOccurred: IGameEvent`）。这是事件/命令契约本身，不算"Domain 内部"。Plan 4 再决定是否投影为 UI 专属 ViewModel。

**踩坑提醒（来自 Plan 1/2）：**

- **不要写 `r.State!.Value`** —— `GameState` 是 sealed record **class**（引用类型），`r.State!` 即可。
- **Domain 纯逻辑层不加日志** —— 日志只在 Application 层（`GameSession`/`PreviewService`/`ReplayController`）。
- **`GameState?` 是可空引用类型**：判空用 `is null` / `is not null`，取值 `r.State!`。
- **构造测试局面用 `with` + `ImmutableArrayExtensions.Replace`** 改棋子位置/墙数。
- **设墙测试注意 P2 起点**：P2 初始在 (4,8)（9×9）或 (3,6)（7×7 Kid）；凡让 P1 走到该格获胜时先把 P2 挪开。
- **TDD 红色阶段对"纯测试/纯脚手架"任务可能不出现**（如 Task 1 脚手架、Task 8 纯测试增强）：被测类型已在前面任务就绪或本任务仅改测试，首跑即绿。属正常，不必强行造红。

## Prerequisites

- .NET 10 SDK（10.0.301 已装）。
- Plan 1+2 已在 master：`src/Quoridor.Domain/`（Core/Path/Rules/Notation/AI）+ `tests/Quoridor.Domain.Tests/`，`dotnet test` 63 通过。
- 工作目录：仓库根。执行时用 `superpowers:using-git-worktrees` 建隔离工作树（每 Plan 一棵，完成后 FF 合并回 master 并清理）。

## 已有 API（Plan 1/2，本计划直接复用，勿改其语义）

- `GameSetup.Create(BoardConfig cfg, int players) → GameState`；`CreateStandard2P/CreateStandard4P/CreateKid2P/CreateKid4P`。
- `BoardConfig.Standard`(9×9) / `BoardConfig.Kid`(7×7)；`BoardConfig.MaxIndex`；`BoardConfig.Variant`。
- `RuleEngine.ValidateAndApply(GameState, IGameCommand) → ApplyResult(GameState? State, ImmutableArray<IGameEvent> Events)`：`State` 非空=已应用，null=拒绝（`Events` 含 `MoveRejected`/`WallRejected`）。`ApplyResult` 是 `readonly record struct`。
- `GameState`（sealed record **class**）：`Config/Players/Pawns/Walls/ActivePlayer/Phase/Winner`，`PawnOf(id)`/`PlayerOf(id)`/`IsFinished`。`Players` 为 `ImmutableArray<PlayerState>`，`Pawns` 为 `ImmutableArray<Pawn>`，`Walls` 为 `ImmutableArray<WallPos>`。
- `PlayerState(PlayerId Id, Cell Start, GoalEdge Goal, int WallsLeft)`。
- `Pawn(PlayerId Owner, Cell Pos)`；`Cell(int Col, int Row)`；`WallPos(Cell Anchor, WallOrient Orient)`。
- `Commands`：`MovePawnCommand(Cell To)`、`PlaceWallCommand(WallPos Wall)`，均实现 `IGameCommand`。
- `Events`：`PawnMoved`/`WallPlaced`/`MoveRejected`/`WallRejected`/`PlayerWon`/`TurnPassed`，均实现 `IGameEvent`。`MoveRejected(PlayerId Who, Cell To, RejectReason Reason)`、`WallRejected(PlayerId Who, WallPos Wall, RejectReason Reason)`。
- `RejectReason`：`NotYourTurn/IllegalMove/BlockedByWall/OffBoard/WallOverlap/WallOutOfBounds/WallBlocksAllPaths/NoWallsLeft/GameFinished`。
- `WallLegality.Validate(GameState, WallPos) → RejectReason?`：null=墙合法（含重叠/越界/可达性检查；**不含墙数**，墙数由 `RuleEngine.ApplyWall` 单独判）。
- `PathFinder.ShortestPath(BoardGraph, Cell, GoalEdge) → PathResult(int Distance, ImmutableArray<Cell> Path)`：`Distance=-1` 不可达。
- `BoardGraph(GameState)`；`.Config`/`.InBounds`/`.HasWallBetween`。
- `NotationService`：`CellToNotation`/`WallToNotation`/`Encode(IReadOnlyList<IGameEvent>, int playerCount)`/`Decode(string)`/`Replay(BoardConfig, int, string)`。`NotationParseException`。
- `IQuoridorAi.Choose(GameState, Difficulty) → IGameCommand`；`GreedyAi`/`MinimaxAi`；`Difficulty { Easy, Medium, Hard }`（Minimax 深度 Easy=1/Medium=2/Hard=3）。

## 文件结构

```
src/Quoridor.Application/
  Quoridor.Application.csproj   -- 引用 Domain
  Logging/
    LogLevel.cs                 -- 枚举 Debug/Info/Warning/Error
    IAppLogger.cs               -- 接口: void Log(LogLevel, string, params object[])
    NullAppLogger.cs            -- 默认空实现
  Seats/
    IPlayer.cs                  -- 座位门槛: Id/IsHuman/ProposeMove
    HumanPlayer.cs              -- 人类座位, ProposeMove 返回 null
    AiPlayer.cs                 -- AI 座位, 包装 IQuoridorAi+Difficulty
    AiPlayerFactory.cs          -- 难度→AI 映射(债9): Easy=Greedy, Medium/Hard=Minimax
  GameSession.cs                -- Submit+事件广播+AI自动驱动+日志+EventLog+Export
  PreviewService.cs             -- PoseWall 只读预览(合法性+各棋子最短路线)
  ReplayController.cs           -- 导入记谱+步进回放(⏮⬅➡⏭)
  README.md
tests/Quoridor.Application.Tests/
  Quoridor.Application.Tests.csproj -- 引用 Application+Domain
  ScaffoldTests.cs              -- 脚手架冒烟
  Logging/NullAppLoggerTests.cs
  Seats/PlayerSeatsTests.cs
  GameSessionTests.cs
  PreviewServiceTests.cs
  ReplayControllerTests.cs
  Integration/GameSessionIntegrationTests.cs
```

依赖方向：`Application` → `Domain`（单向）。`Application.Tests` → `Application` + `Domain`。

---

## Task 1: 项目脚手架（Application + Application.Tests + slnx）

**Files:**
- Create: `src/Quoridor.Application/Quoridor.Application.csproj`
- Create: `tests/Quoridor.Application.Tests/Quoridor.Application.Tests.csproj`
- Create: `tests/Quoridor.Application.Tests/ScaffoldTests.cs`
- Modify: `Quoridor.slnx`

- [ ] **Step 1: 创建 Application 类库项目**

`src/Quoridor.Application/Quoridor.Application.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Quoridor.Domain\Quoridor.Domain.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: 创建 Application.Tests 测试项目**

`tests/Quoridor.Application.Tests/Quoridor.Application.Tests.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Quoridor.Application\Quoridor.Application.csproj" />
    <ProjectReference Include="..\..\src\Quoridor.Domain\Quoridor.Domain.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: 创建脚手架冒烟测试**

`tests/Quoridor.Application.Tests/ScaffoldTests.cs`：

```csharp
namespace Quoridor.Application.Tests;

public class ScaffoldTests
{
    [Fact]
    public void TestHarnessRuns()
    {
        Assert.Equal(4, 2 + 2);
    }
}
```

- [ ] **Step 4: 更新 slnx 加入两个新项目**

把 `Quoridor.slnx` 改为：

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/Quoridor.Domain/Quoridor.Domain.csproj" />
    <Project Path="src/Quoridor.Application/Quoridor.Application.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/Quoridor.Domain.Tests/Quoridor.Domain.Tests.csproj" />
    <Project Path="tests/Quoridor.Application.Tests/Quoridor.Application.Tests.csproj" />
  </Folder>
</Solution>
```

- [ ] **Step 5: 构建并测试，验证通过**

Run: `dotnet test Quoridor.slnx`
Expected: PASS（Domain 63 + Application 1 = 64）。若 slnx 命令不可用，退回 `dotnet test tests/Quoridor.Application.Tests` 单独验证（应 PASS 1），再 `dotnet test tests/Quoridor.Domain.Tests` 确认 63 不回归。

- [ ] **Step 6: 提交**

```bash
git add -A
git commit -m "chore(app): 添加 Quoridor.Application 类库与测试项目脚手架"
```

---

## Task 2: IAppLogger + NullAppLogger（轻量日志）

**Files:**
- Create: `src/Quoridor.Application/Logging/LogLevel.cs`
- Create: `src/Quoridor.Application/Logging/IAppLogger.cs`
- Create: `src/Quoridor.Application/Logging/NullAppLogger.cs`
- Test: `tests/Quoridor.Application.Tests/Logging/NullAppLoggerTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Application.Tests/Logging/NullAppLoggerTests.cs`：

```csharp
using Quoridor.Application.Logging;

namespace Quoridor.Application.Tests.Logging;

public class NullAppLoggerTests
{
    [Fact]
    public void Null_logger_swallows_messages_without_throwing()
    {
        IAppLogger logger = new NullAppLogger();
        logger.Log(LogLevel.Info, "anything {0}", 1);
        logger.Log(LogLevel.Error, "boom");
    }

    [Fact]
    public void Capturing_logger_receives_message_via_interface_contract()
    {
        // 用一个测试专用捕获实现, 验证 IAppLogger 契约把消息传给实现者
        var cap = new CapturingLogger();
        cap.Log(LogLevel.Warning, "hi");
        Assert.Single(cap.Messages);
        Assert.Equal("hi", cap.Messages[0]);
        Assert.Equal(LogLevel.Warning, cap.Levels[0]);
    }

    private sealed class CapturingLogger : IAppLogger
    {
        public List<string> Messages { get; } = new();
        public List<LogLevel> Levels { get; } = new();

        public void Log(LogLevel level, string message, params object[] args)
        {
            Levels.Add(level);
            Messages.Add(message);
        }
    }
}
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test tests/Quoridor.Application.Tests`
Expected: FAIL（`IAppLogger`/`LogLevel`/`NullAppLogger` 未定义，CS0246）。

- [ ] **Step 3: 写实现**

`src/Quoridor.Application/Logging/LogLevel.cs`：

```csharp
namespace Quoridor.Application.Logging;

public enum LogLevel { Debug, Info, Warning, Error }
```

`src/Quoridor.Application/Logging/IAppLogger.cs`：

```csharp
namespace Quoridor.Application.Logging;

public interface IAppLogger
{
    void Log(LogLevel level, string message, params object[] args);
}
```

`src/Quoridor.Application/Logging/NullAppLogger.cs`：

```csharp
namespace Quoridor.Application.Logging;

public sealed class NullAppLogger : IAppLogger
{
    public static NullAppLogger Instance { get; } = new();

    public void Log(LogLevel level, string message, params object[] args) { }
}
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test tests/Quoridor.Application.Tests`
Expected: PASS（1 旧 + 2 新 = 3）。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(app): 添加轻量 IAppLogger 与 NullAppLogger"
```

---

## Task 3: 座位抽象 IPlayer / HumanPlayer / AiPlayer / AiPlayerFactory（债9）

**Files:**
- Create: `src/Quoridor.Application/Seats/IPlayer.cs`
- Create: `src/Quoridor.Application/Seats/HumanPlayer.cs`
- Create: `src/Quoridor.Application/Seats/AiPlayer.cs`
- Create: `src/Quoridor.Application/Seats/AiPlayerFactory.cs`
- Test: `tests/Quoridor.Application.Tests/Seats/PlayerSeatsTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Application.Tests/Seats/PlayerSeatsTests.cs`：

```csharp
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;
using Quoridor.Application.Seats;
using Xunit;

namespace Quoridor.Application.Tests.Seats;

public class PlayerSeatsTests
{
    [Fact]
    public void Human_player_is_human_and_proposes_null()
    {
        IPlayer p = new HumanPlayer(PlayerId.P1);
        Assert.True(p.IsHuman);
        Assert.Equal(PlayerId.P1, p.Id);
        Assert.Null(p.ProposeMove(GameSetup.CreateStandard2P()));
    }

    [Fact]
    public void Ai_player_is_not_human_and_proposes_legal_command()
    {
        IPlayer p = new AiPlayer(PlayerId.P2, new GreedyAi(), Difficulty.Easy);
        Assert.False(p.IsHuman);
        var state = GameSetup.CreateStandard2P();
        var cmd = p.ProposeMove(state);
        Assert.NotNull(cmd);
        var r = RuleEngine.ValidateAndApply(state, cmd!);
        Assert.NotNull(r.State);  // AI 永不下非法手
    }

    [Fact]
    public void Ai_player_returns_null_on_finished_state()  // 债7 防御
    {
        var finished = GameSetup.CreateStandard2P() with { Phase = Phase.Finished, Winner = PlayerId.P1 };
        IPlayer p = new AiPlayer(PlayerId.P1, new GreedyAi(), Difficulty.Easy);
        Assert.Null(p.ProposeMove(finished));
    }

    [Fact]
    public void Factory_easy_uses_greedy_and_returns_legal_command()  // 债9 映射
    {
        var state = GameSetup.CreateStandard2P();
        foreach (Difficulty d in new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard })
        {
            IPlayer p = AiPlayerFactory.Create(PlayerId.P1, d);
            Assert.False(p.IsHuman);
            var cmd = p.ProposeMove(state);
            Assert.NotNull(cmd);
            Assert.NotNull(RuleEngine.ValidateAndApply(state, cmd!).State);  // 各档均合法
        }
    }

    [Fact]
    public void Factory_easy_advances_on_empty_board()
    {
        // Easy=Greedy: 在空盘上 Greedy 选评估最大的推进手, P1 目标北(row 增) → To.Row>0
        var state = GameSetup.CreateStandard2P();
        IPlayer p = AiPlayerFactory.Create(PlayerId.P1, Difficulty.Easy);
        var cmd = p.ProposeMove(state)!;
        var move = Assert.IsType<MovePawnCommand>(cmd);
        Assert.True(move.To.Row > 0, $"Easy 档应向北推进, 实际 To={move.To}");
    }
}
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test tests/Quoridor.Application.Tests`
Expected: FAIL（`IPlayer`/`HumanPlayer`/`AiPlayer`/`AiPlayerFactory` 未定义）。

- [ ] **Step 3: 写实现**

`src/Quoridor.Application/Seats/IPlayer.cs`：

```csharp
using Quoridor.Domain.Core;

namespace Quoridor.Application.Seats;

/// <summary>座位门槛: 人与 AI 都实现"在给定状态产出一个命令(或 null 表示等待外部输入)"的契约。</summary>
public interface IPlayer
{
    PlayerId Id { get; }
    bool IsHuman { get; }

    /// <summary>返回该座位的拟走命令; 人类返回 null(经 GameSession.Submit 提交), AI 返回 IQuoridorAi.Choose 结果。</summary>
    IGameCommand? ProposeMove(GameState state);
}
```

`src/Quoridor.Application/Seats/HumanPlayer.cs`：

```csharp
using Quoridor.Domain.Core;

namespace Quoridor.Application.Seats;

public sealed class HumanPlayer : IPlayer
{
    public PlayerId Id { get; }
    public bool IsHuman => true;

    public HumanPlayer(PlayerId id) => Id = id;

    public IGameCommand? ProposeMove(GameState state) => null;
}
```

`src/Quoridor.Application/Seats/AiPlayer.cs`：

```csharp
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;

namespace Quoridor.Application.Seats;

public sealed class AiPlayer : IPlayer
{
    private readonly IQuoridorAi _ai;
    private readonly Difficulty _difficulty;

    public PlayerId Id { get; }
    public bool IsHuman => false;

    public AiPlayer(PlayerId id, IQuoridorAi ai, Difficulty difficulty)
    {
        Id = id;
        _ai = ai;
        _difficulty = difficulty;
    }

    public IGameCommand? ProposeMove(GameState state)
    {
        // 债7: 入口守卫, 对已结束局面不调 AI(避免 RuleEngine 以 GameFinished 拒绝)
        if (state.IsFinished) return null;
        return _ai.Choose(state, _difficulty);
    }
}
```

`src/Quoridor.Application/Seats/AiPlayerFactory.cs`：

```csharp
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;

namespace Quoridor.Application.Seats;

/// <summary>难度→AI 实现映射(债9): Easy=GreedyAi, Medium/Hard=MinimaxAi(深度由 Difficulty 决定)。</summary>
public static class AiPlayerFactory
{
    public static AiPlayer Create(PlayerId id, Difficulty difficulty)
    {
        IQuoridorAi impl = difficulty == Difficulty.Easy
            ? new GreedyAi()
            : new MinimaxAi();
        return new AiPlayer(id, impl, difficulty);
    }
}
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test tests/Quoridor.Application.Tests`
Expected: PASS（3 + 5 = 8）。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(app): 添加 IPlayer 座位抽象与 AiPlayerFactory 难度映射"
```

---

## Task 4: GameSession（命令调度 + 事件广播 + AI 自动驱动 + 日志，债7）

**Files:**
- Create: `src/Quoridor.Application/GameSession.cs`
- Test: `tests/Quoridor.Application.Tests/GameSessionTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Application.Tests/GameSessionTests.cs`：

```csharp
using System.Collections.Generic;
using Quoridor.Application.Logging;
using Quoridor.Application.Seats;
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Xunit;

namespace Quoridor.Application.Tests;

public class GameSessionTests
{
    [Fact]
    public void Human_submit_advances_state_and_broadcasts_events()
    {
        var session = NewHumanVsHuman();
        var events = new List<IGameEvent>();
        session.EventOccurred += e => events.Add(e);

        var r = session.Submit(new MovePawnCommand(new Cell(4, 1)));  // P1 北上 e2

        Assert.NotNull(r.State);
        Assert.Equal(new Cell(4, 1), session.State.PawnOf(PlayerId.P1).Pos);
        Assert.Equal(PlayerId.P2, session.State.ActivePlayer);
        Assert.Contains(events, e => e is PawnMoved);
        Assert.Contains(events, e => e is TurnPassed);
    }

    [Fact]
    public void Illegal_submit_rejected_state_unchanged()
    {
        var session = NewHumanVsHuman();
        var before = session.State;
        var events = new List<IGameEvent>();
        session.EventOccurred += e => events.Add(e);

        var r = session.Submit(new MovePawnCommand(new Cell(4, 2)));  // 跨两格非法

        Assert.Null(r.State);
        Assert.Same(before, session.State);  // 状态不变
        Assert.Contains(events, e => e is MoveRejected);
    }

    [Fact]
    public void Submit_when_finished_is_noop()  // 债7: 终局后不再推进/AI 不被调用
    {
        var session = NewHumanVsHuman();
        // 手动把状态置为终局
        var finished = session.State with { Phase = Phase.Finished, Winner = PlayerId.P1 };
        typeof(GameSession).GetProperty(nameof(GameSession.State))!
            .SetValue(session, finished);  // 测试钩子: 强制终局
        var events = new List<IGameEvent>();
        session.EventOccurred += e => events.Add(e);

        session.Submit(new MovePawnCommand(new Cell(4, 1)));

        Assert.True(session.State.IsFinished);
        Assert.Empty(events);  // 终局后不广播
    }

    [Fact]
    public void Ai_seat_auto_drives_after_human_move()
    {
        // P1 人类, P2 AI(Easy)。人类走一手后, AI 应自动跟进一手并回到人类回合。
        var seats = new IPlayer[]
        {
            new HumanPlayer(PlayerId.P1),
            AiPlayerFactory.Create(PlayerId.P2, Difficulty.Easy),
        };
        var session = new GameSession(BoardConfig.Standard, seats);
        var events = new List<IGameEvent>();
        session.EventOccurred += e => events.Add(e);

        session.Start();  // P1 人类 → 不驱动
        Assert.Empty(events);  // 起手人类, 无 AI 事件

        session.Submit(new MovePawnCommand(new Cell(4, 1)));  // P1 走, 然后 P2 AI 自动

        Assert.Contains(events, e => e is PawnMoved pm && pm.Who == PlayerId.P1);
        Assert.Contains(events, e => e is PawnMoved pm && pm.Who == PlayerId.P2);
        Assert.Equal(PlayerId.P1, session.State.ActivePlayer);  // AI 走完回到 P1
        Assert.NotEqual(new Cell(4, 8), session.State.PawnOf(PlayerId.P2).Pos);  // P2 已动
    }

    [Fact]
    public void Ai_vs_ai_kid_easy_terminates_with_winner()
    {
        var seats = new IPlayer[]
        {
            AiPlayerFactory.Create(PlayerId.P1, Difficulty.Easy),
            AiPlayerFactory.Create(PlayerId.P2, Difficulty.Easy),
        };
        var session = new GameSession(BoardConfig.Kid, seats);

        session.Start();

        Assert.True(session.State.IsFinished);
        Assert.NotNull(session.State.Winner);
    }

    [Fact]
    public void Logger_records_submit_lifecycle()
    {
        var cap = new CapturingLogger();
        var session = new GameSession(BoardConfig.Standard, new IPlayer[]
        {
            new HumanPlayer(PlayerId.P1),
            new HumanPlayer(PlayerId.P2),
        }, cap);

        session.Submit(new MovePawnCommand(new Cell(4, 1)));

        Assert.Contains(cap.Messages, m => m.Contains("Submit") && m.Contains("入口"));
        Assert.Contains(cap.Messages, m => m.Contains("应用成功"));
    }

    [Fact]
    public void Event_log_and_export_reflect_played_plies()
    {
        var session = NewHumanVsHuman();
        session.Submit(new MovePawnCommand(new Cell(4, 1)));  // P1 e2
        session.Submit(new MovePawnCommand(new Cell(4, 7)));  // P2 e8

        var notation = session.Export();
        Assert.Equal("1. e2 e8", notation);
    }

    private static GameSession NewHumanVsHuman() => new(BoardConfig.Standard, new IPlayer[]
    {
        new HumanPlayer(PlayerId.P1),
        new HumanPlayer(PlayerId.P2),
    });

    private sealed class CapturingLogger : IAppLogger
    {
        public List<string> Messages { get; } = new();
        public void Log(LogLevel level, string message, params object[] args) => Messages.Add(message);
    }
}
```

> 注：`Submit_when_finished_is_noop` 用反射强制置 `State` 为终局，仅为测试隔离。若反射写私有 set 不便，可改为给 `GameSession` 加 `internal` 测试钩子 `SetStateForTest(GameState s)`（仅在 DEBUG 或 InternalsVisibleTo 下可见）。本计划采用反射以避免污染生产 API。

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test tests/Quoridor.Application.Tests`
Expected: FAIL（`GameSession` 未定义）。

- [ ] **Step 3: 写实现**

`src/Quoridor.Application/GameSession.cs`：

```csharp
using System.Collections.Generic;
using System.Collections.Immutable;
using Quoridor.Application.Logging;
using Quoridor.Application.Seats;
using Quoridor.Domain.Core;
using Quoridor.Domain.Notation;
using Quoridor.Domain.Rules;

namespace Quoridor.Application;

/// <summary>
/// 对局编排中枢: 持有当前 GameState + 座位表, 人手与 AI 走同一 Submit→ValidateAndApply→广播 通道。
/// AI 座位在人手之后由本类自动驱动(循环到人类回合或终局)。
/// </summary>
public sealed class GameSession
{
    private readonly BoardConfig _cfg;
    private readonly IReadOnlyDictionary<PlayerId, IPlayer> _seats;
    private readonly IAppLogger _logger;
    private readonly List<IGameEvent> _eventLog = new();

    private const int DefaultMaxPlies = 1000;  // AI 驱动安全上限, 防失控

    public GameState State { get; private set; }
    public IReadOnlyList<IGameEvent> EventLog => _eventLog;

    /// <summary>事件广播: 每个已发生的事件(含 Rejected)都会经此通知订阅者。</summary>
    public event Action<IGameEvent>? EventOccurred;

    public GameSession(BoardConfig cfg, IReadOnlyList<IPlayer> seats, IAppLogger? logger = null)
    {
        _cfg = cfg;
        _logger = logger ?? NullAppLogger.Instance;
        _seats = BuildSeatMap(seats);
        State = GameSetup.Create(cfg, seats.Count);
        _logger.Log(LogLevel.Info, "GameSession 构造 cfg={Variant} players={N}", cfg.Variant, seats.Count);
    }

    /// <summary>启动对局: 若起手座位是 AI 则自动驱动。人类起手为空操作。</summary>
    public void Start(int maxPlies = DefaultMaxPlies)
    {
        _logger.Log(LogLevel.Info, "对局开始");
        DriveAi(maxPlies);
    }

    /// <summary>提交一手命令(人或外部来源)。合法则替换状态并广播, 随后自动驱动后续 AI 座位。</summary>
    public RuleEngine.ApplyResult Submit(IGameCommand command)
    {
        _logger.Log(LogLevel.Info, "Submit 入口 active={Active} cmd={Cmd}", State.ActivePlayer, command);

        if (State.IsFinished)
        {
            _logger.Log(LogLevel.Warning, "Submit 跳过: 对局已终局");
            return new RuleEngine.ApplyResult(null, ImmutableArray<IGameEvent>.Empty);
        }

        var r = RuleEngine.ValidateAndApply(State, command);
        Broadcast(r.Events);

        if (r.State is null)
        {
            _logger.Log(LogLevel.Warning, "Submit 被规则拒绝 events={Events}", string.Join(',', r.Events));
            return r;
        }

        State = r.State!;
        _logger.Log(LogLevel.Info, "Submit 应用成功 新活跃={Active}", State.ActivePlayer);

        DriveAi(DefaultMaxPlies);  // 自动驱动后续 AI 座位
        return r;
    }

    /// <summary>导出当前已走完的记谱串(仅含已应用的走子/设墙)。</summary>
    public string Export() => NotationService.Encode(_eventLog, State.Players.Length);

    private void DriveAi(int maxPlies)
    {
        int plies = 0;
        while (!State.IsFinished && plies < maxPlies)
        {
            if (!_seats.TryGetValue(State.ActivePlayer, out var seat))
            {
                _logger.Log(LogLevel.Warning, "DriveAi: 活跃玩家无座位 {Active}, 停止", State.ActivePlayer);
                return;
            }
            if (seat.IsHuman) return;  // 等人类 Submit

            var cmd = seat.ProposeMove(State);
            if (cmd is null) return;  // 债7: AI 拒绝(含已终局防御)

            var r = RuleEngine.ValidateAndApply(State, cmd);
            Broadcast(r.Events);
            if (r.State is null)
            {
                _logger.Log(LogLevel.Error, "DriveAi: AI 产出非法命令 {Cmd}, 停止", cmd);
                return;
            }
            State = r.State!;
            _logger.Log(LogLevel.Debug, "DriveAi: AI({Who}) 走 {Cmd}", seat.Id, cmd);
            plies++;
        }

        if (State.IsFinished)
            _logger.Log(LogLevel.Info, "对局终局 winner={Winner}", State.Winner);
    }

    private void Broadcast(ImmutableArray<IGameEvent> events)
    {
        foreach (var e in events)
        {
            _eventLog.Add(e);
            EventOccurred?.Invoke(e);
        }
    }

    private static IReadOnlyDictionary<PlayerId, IPlayer> BuildSeatMap(IReadOnlyList<IPlayer> seats)
    {
        var dict = new Dictionary<PlayerId, IPlayer>();
        foreach (var s in seats) dict[s.Id] = s;
        return dict;
    }
}
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test tests/Quoridor.Application.Tests`
Expected: PASS（8 + 7 = 15）。`Ai_vs_ai_kid_easy_terminates_with_winner` 应 <3s。若超时，先确认 GreedyAi 自对弈正常（Plan 2 已验证），**不改测试迎合**——报 DONE_WITH_CONCERNS。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(app): 添加 GameSession 命令调度/事件广播/AI 自动驱动/日志"
```

---

## Task 5: PreviewService（设墙悬浮预览，只读）

**Files:**
- Create: `src/Quoridor.Application/PreviewService.cs`
- Test: `tests/Quoridor.Application.Tests/PreviewServiceTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Application.Tests/PreviewServiceTests.cs`：

```csharp
using Quoridor.Domain.Core;
using Quoridor.Domain.Path;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Application.Tests;

public class PreviewServiceTests
{
    [Fact]
    public void Legal_wall_returns_routes_for_all_pawns()
    {
        var state = GameSetup.CreateStandard2P();
        var wall = new WallPos(new Cell(4, 3), WallOrient.Horizontal);  // e4h, 不切断路径

        var r = PreviewService.PoseWall(state, wall);

        Assert.True(r.Legal);
        Assert.Null(r.Reason);
        Assert.Equal(2, r.Routes.Length);  // P1 + P2
        Assert.Contains(r.Routes, x => x.Pawn == PlayerId.P1 && x.Steps >= 0);
        Assert.Contains(r.Routes, x => x.Pawn == PlayerId.P2 && x.Steps >= 0);
    }

    [Fact]
    public void Wall_blocking_all_paths_is_illegal()
    {
        // 角落盒: P1 在 (0,0), V(0,0) 阻东 + H(0,0) 阻北 → P1 不可达北目标
        var s = GameSetup.CreateStandard2P();
        s = s with
        {
            Pawns = s.Pawns.Replace(s.PawnOf(PlayerId.P1), s.PawnOf(PlayerId.P1) with { Pos = new Cell(0, 0) }),
        };
        s = s with { Walls = s.Walls.Add(new WallPos(new Cell(0, 0), WallOrient.Vertical)) };

        var r = PreviewService.PoseWall(s, new WallPos(new Cell(0, 0), WallOrient.Horizontal));

        Assert.False(r.Legal);
        Assert.Equal(RejectReason.WallBlocksAllPaths, r.Reason);
    }

    [Fact]
    public void Overlapping_wall_is_illegal()
    {
        var s = GameSetup.CreateStandard2P();
        s = s with { Walls = s.Walls.Add(new WallPos(new Cell(0, 0), WallOrient.Horizontal)) };

        var r = PreviewService.PoseWall(s, new WallPos(new Cell(0, 0), WallOrient.Horizontal));

        Assert.False(r.Legal);
        Assert.Equal(RejectReason.WallOverlap, r.Reason);
    }

    [Fact]
    public void Preview_does_not_mutate_original_state()
    {
        var state = GameSetup.CreateStandard2P();
        var wallsBefore = state.Walls.Length;

        PreviewService.PoseWall(state, new WallPos(new Cell(4, 3), WallOrient.Horizontal));

        Assert.Equal(wallsBefore, state.Walls.Length);  // 原状态墙数不变
    }

    [Fact]
    public void Wall_lengthening_opponent_increases_steps()
    {
        var state = GameSetup.CreateStandard2P();
        var baseline = PreviewService.PoseWall(state, new WallPos(new Cell(4, 3), WallOrient.Horizontal));
        var p2Baseline = StepsOf(baseline, PlayerId.P2);

        // 在 P2(4,8) 南侧加水平墙, 拉长 P2 到南目标的路径
        var blocked = state with { Walls = state.Walls.Add(new WallPos(new Cell(3, 7), WallOrient.Horizontal)) };
        var after = PreviewService.PoseWall(blocked, new WallPos(new Cell(4, 3), WallOrient.Horizontal));
        var p2After = StepsOf(after, PlayerId.P2);

        Assert.True(p2After >= p2Baseline, $"P2 步数应不减少, base={p2Baseline} after={p2After}");
    }

    private static int StepsOf(PreviewResult r, PlayerId id)
    {
        foreach (var x in r.Routes) if (x.Pawn == id) return x.Steps;
        return -1;
    }
}
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test tests/Quoridor.Application.Tests`
Expected: FAIL（`PreviewService`/`PreviewResult`/`RoutePreview` 未定义）。

- [ ] **Step 3: 写实现**

`src/Quoridor.Application/PreviewService.cs`：

```csharp
using System.Collections.Generic;
using System.Collections.Immutable;
using Quoridor.Domain.Core;
using Quoridor.Domain.Path;
using Quoridor.Domain.Rules;

namespace Quoridor.Application;

public readonly record struct RoutePreview(PlayerId Pawn, int Steps, ImmutableArray<Cell> Path);

public readonly record struct PreviewResult(bool Legal, RejectReason? Reason, ImmutableArray<RoutePreview> Routes);

/// <summary>设墙悬浮预览(只读): 不改真实状态, 临时叠加一面墙算各棋子最短路线/步数与合法性。</summary>
public static class PreviewService
{
    public static PreviewResult PoseWall(GameState state, WallPos wall)
    {
        // 合法性: 用 WallLegality(含重叠/越界/可达性, 不含墙数——预览忽略墙数)
        var reason = WallLegality.Validate(state, wall);
        if (reason is not null)
            return new PreviewResult(false, reason, ImmutableArray<RoutePreview>.Empty);

        // 临时叠加墙算路线(不改原 state)
        var previewed = state with { Walls = state.Walls.Add(wall) };
        var graph = new BoardGraph(previewed);

        var routes = new List<RoutePreview>();
        foreach (var pawn in previewed.Pawns)
        {
            var goal = previewed.PlayerOf(pawn.Owner).Goal;
            var pr = PathFinder.ShortestPath(graph, pawn.Pos, goal);
            routes.Add(new RoutePreview(pawn.Owner, pr.Distance, pr.Path));
        }
        return new PreviewResult(true, null, routes.ToImmutableArray());
    }
}
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test tests/Quoridor.Application.Tests`
Expected: PASS（15 + 5 = 20）。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(app): 添加 PreviewService 设墙悬浮只读预览"
```

---

## Task 6: Domain 债1+2 — NotationService 范围校验 + 续谱占位

**Files:**
- Modify: `src/Quoridor.Domain/Notation/NotationService.cs`
- Modify: `tests/Quoridor.Domain.Tests/Notation/NotationDecodeTests.cs`

> 本任务改动 Domain 的 NotationService（纯逻辑，不破坏零 Godot 依赖）。债1：`ParseCell` 加坐标范围校验抛精确异常；债2：`Decode` 支持 `3... e3h` 续谱占位。

- [ ] **Step 1: 写失败测试**

在 `tests/Quoridor.Domain.Tests/Notation/NotationDecodeTests.cs` 末尾追加（保留原有测试）：

```csharp
    [Fact]
    public void Decode_with_cfg_rejects_out_of_range_column()
    {
        // 债1: z9 列越界, cfg 感知解析应抛精确异常而非靠规则拒绝
        var ex = Assert.Throws<NotationParseException>(() =>
            NotationService.Decode("1. z9 e8", BoardConfig.Standard));
        Assert.Contains("列越界", ex.Message);
    }

    [Fact]
    public void Decode_with_cfg_rejects_out_of_range_row()
    {
        // e10 → row=9 越出 9×9(MaxIndex=8)
        var ex = Assert.Throws<NotationParseException>(() =>
            NotationService.Decode("1. e10", BoardConfig.Standard));
        Assert.Contains("行越界", ex.Message);
    }

    [Fact]
    public void Decode_handles_continuation_marker()  // 债2: 3... e3h
    {
        var cmds = NotationService.Decode("3... e3h");
        var wall = Assert.Single(cmds);
        var pw = Assert.IsType<PlaceWallCommand>(wall);
        Assert.Equal(new WallPos(new Cell(4, 2), WallOrient.Horizontal), pw.Wall);
    }

    [Fact]
    public void Decode_continuation_marker_with_both_sides()
    {
        // 3. e6h(白) 3... e3h(黑续谱)
        var cmds = NotationService.Decode("3. e6h 3... e3h");
        Assert.Equal(2, cmds.Length);
        Assert.All(cmds, c => Assert.IsType<PlaceWallCommand>(c));
    }

    [Fact]
    public void Decode_pure_continuation_marker_yields_no_commands()
    {
        Assert.Empty(NotationService.Decode("3..."));
        Assert.Empty(NotationService.Decode("3."));
    }
```

并把原 `Decode_invalid_token_throws_parse_error` 的注释与断言更新为 cfg 感知精确语义：

```csharp
    [Fact]
    public void Decode_invalid_token_throws_parse_error()
    {
        // 债1: z9 列越界, Replay 内部用 cfg 感知 Decode → 抛精确"列越界"异常
        var ex = Assert.Throws<NotationParseException>(() =>
            NotationService.Replay(BoardConfig.Standard, 2, "1. z9 e8"));
        Assert.Contains("列越界", ex.Message);
    }
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test tests/Quoridor.Domain.Tests`
Expected: FAIL（`Decode(string, BoardConfig)` 重载不存在；`3...` 解析报错）。新增 5 个测试失败。

- [ ] **Step 3: 写实现**

把 `src/Quoridor.Domain/Notation/NotationService.cs` 的 `Decode` / `Replay` / `ParseCell` 段替换为（保留 `CellToNotation`/`WallToNotation`/`Encode` 不变）：

```csharp
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
```

> 注意：原 `ParseCell(string)` 私有方法签名改为 `ParseCell(string, BoardConfig?)`。原 `Decode(string)` 改为转发到 `DecodeCore(notation, null)`（不校验范围，向后兼容；现有 `NotationDecodeTests.Decode_parses_moves_and_walls` 等仍走此路径通过）。

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test tests/Quoridor.Domain.Tests`
Expected: PASS（63 原有 + 5 新增 = 68；原有 `Decode_invalid_token_throws_parse_error` 断言更新后仍通过）。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "fix(domain): NotationService 坐标范围校验与续谱占位解析(债1,2)"
```

---

## Task 7: ReplayController（导入记谱 + 步进回放）

**Files:**
- Create: `src/Quoridor.Application/ReplayController.cs`
- Test: `tests/Quoridor.Application.Tests/ReplayControllerTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Application.Tests/ReplayControllerTests.cs`：

```csharp
using Quoridor.Domain.Core;
using Xunit;

namespace Quoridor.Application.Tests;

public class ReplayControllerTests
{
    private const string TwoPGame = "1. e2 e8 2. e3 e7";

    [Fact]
    public void Construct_parses_total_and_starts_at_initial_state()
    {
        var rc = new ReplayController(BoardConfig.Standard, 2, TwoPGame);
        Assert.Equal(4, rc.Total);
        Assert.True(rc.AtStart);
        Assert.False(rc.AtEnd);
        Assert.Equal(new Cell(4, 0), rc.Current.PawnOf(PlayerId.P1).Pos);  // 初始 e1
    }

    [Fact]
    public void Step_forward_advances_cursor_and_state()
    {
        var rc = new ReplayController(BoardConfig.Standard, 2, TwoPGame);
        Assert.True(rc.StepForward());
        Assert.Equal(1, rc.Cursor);
        Assert.Equal(new Cell(4, 1), rc.Current.PawnOf(PlayerId.P1).Pos);  // e2
    }

    [Fact]
    public void At_end_step_forward_returns_false()
    {
        var rc = new ReplayController(BoardConfig.Standard, 2, TwoPGame);
        for (int i = 0; i < 4; i++) rc.StepForward();
        Assert.True(rc.AtEnd);
        Assert.False(rc.StepForward());
    }

    [Fact]
    public void Step_back_rebuilds_state_to_prior_cursor()
    {
        var rc = new ReplayController(BoardConfig.Standard, 2, TwoPGame);
        rc.GoTo(3);  // 走到第 3 手后
        Assert.Equal(3, rc.Cursor);

        Assert.True(rc.StepBack());

        Assert.Equal(2, rc.Cursor);
        // 第 2 手后: P1 在 e2(已走第1手), P2 在 e8(已走第2手)... 实为走到第2手末
        Assert.Equal(new Cell(4, 1), rc.Current.PawnOf(PlayerId.P1).Pos);
        Assert.Equal(new Cell(4, 7), rc.Current.PawnOf(PlayerId.P2).Pos);
    }

    [Fact]
    public void Reset_returns_to_start()
    {
        var rc = new ReplayController(BoardConfig.Standard, 2, TwoPGame);
        rc.GoTo(4);
        rc.Reset();
        Assert.True(rc.AtStart);
        Assert.Equal(0, rc.Cursor);
    }

    [Fact]
    public void Go_to_jump_arbitrary_index()
    {
        var rc = new ReplayController(BoardConfig.Standard, 2, TwoPGame);
        rc.GoTo(2);
        Assert.Equal(2, rc.Cursor);
        Assert.Equal(new Cell(4, 1), rc.Current.PawnOf(PlayerId.P1).Pos);
        Assert.Equal(new Cell(4, 7), rc.Current.PawnOf(PlayerId.P2).Pos);
        Assert.Equal(PlayerId.P1, rc.Current.ActivePlayer);
    }

    [Fact]
    public void Invalid_replay_move_throws()
    {
        // e3v 在初始局面合法, 但若记谱里某手非法应抛 NotationParseException
        Assert.Throws<NotationParseException>(() =>
            new ReplayController(BoardConfig.Standard, 2, "1. e2 e8 2. e3 e7 e3"));
        // 最后一手 e3: P1 已在 e3, 再走到 e3 自身 → 非法
    }
}
```

> 注：最后一例 `... e3 e3`——第 4 手 P1 已在 (4,2)=e3，第 5 手再 `e3` 即原地不动，`MoveLegality.LegalTargets` 不含当前位置 → `RuleEngine` 拒绝 → `ReplayController.StepForward` 抛 `NotationParseException`。构造时即 `GoTo`/解析？构造不预跑，仅 `StepForward` 时抛。故断言改为对构造后 StepForward 抛。见下实现：构造只 Decode，不预 apply。因此该测试应改为构造后 `StepForward()` 5 次时抛。修正测试：

```csharp
    [Fact]
    public void Invalid_replay_move_throws_on_step()
    {
        var rc = new ReplayController(BoardConfig.Standard, 2, "1. e2 e8 2. e3 e3");
        rc.StepForward(); rc.StepForward(); rc.StepForward(); rc.StepForward();  // 走完 4 手合法部分
        Assert.Throws<NotationParseException>(() => rc.StepForward());  // 第5手 e3 非法
    }
```

（用上面这条替换前一个 `Invalid_replay_move_throws` 版本。）

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test tests/Quoridor.Application.Tests`
Expected: FAIL（`ReplayController`/`NotationParseException` 引用未定义；`ReplayController` 不存在）。

- [ ] **Step 3: 写实现**

`src/Quoridor.Application/ReplayController.cs`：

```csharp
using System.Collections.Immutable;
using Quoridor.Domain.Core;
using Quoridor.Domain.Notation;
using Quoridor.Domain.Rules;

namespace Quoridor.Application;

/// <summary>记谱回放控制器: 导入记谱串, 提供 ⏮⬅➡⏭ 步进与跳转。命令不可逆, StepBack 通过从头重放实现。</summary>
public sealed class ReplayController
{
    private readonly BoardConfig _cfg;
    private readonly int _players;
    private readonly ImmutableArray<IGameCommand> _cmds;
    private GameState _state;
    private int _cursor;

    public ReplayController(BoardConfig cfg, int players, string notation)
    {
        _cfg = cfg;
        _players = players;
        _cmds = NotationService.Decode(notation, cfg);  // cfg 感知: 越界在此抛精确异常
        _state = GameSetup.Create(cfg, players);
        _cursor = 0;
    }

    public GameState Current => _state;
    public int Cursor => _cursor;
    public int Total => _cmds.Length;
    public bool AtStart => _cursor == 0;
    public bool AtEnd => _cursor == _cmds.Length;

    public void Reset()
    {
        _state = GameSetup.Create(_cfg, _players);
        _cursor = 0;
    }

    public bool StepForward()
    {
        if (_cursor >= _cmds.Length) return false;
        var r = RuleEngine.ValidateAndApply(_state, _cmds[_cursor]);
        if (r.State is null)
            throw new NotationParseException($"回放第 {_cursor + 1} 手非法: {_cmds[_cursor]}");
        _state = r.State!;
        _cursor++;
        return true;
    }

    public bool StepBack()
    {
        if (_cursor == 0) return false;
        int target = _cursor - 1;
        Reset();
        for (int i = 0; i < target; i++) StepForward();
        return true;
    }

    public void GoTo(int index)
    {
        if (index < 0 || index > _cmds.Length)
            throw new System.ArgumentOutOfRangeException(nameof(index));
        Reset();
        for (int i = 0; i < index; i++) StepForward();
    }
}
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test tests/Quoridor.Application.Tests`
Expected: PASS（20 + 6 = 26）。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(app): 添加 ReplayController 记谱导入与步进回放"
```

---

## Task 8: Domain 债4+5 — 删脚手架 + 断言增强 + 封死测试注释

**Files:**
- Delete: `tests/Quoridor.Domain.Tests/SmokeTests.cs`
- Modify: `tests/Quoridor.Domain.Tests/Rules/RuleEngineTests.cs`
- Modify: `tests/Quoridor.Domain.Tests/Path/ReachabilityTests.cs`

> 纯测试清理，无新生产码。TDD 红色阶段可能不出现（仅改测试），首跑即绿——属正常。

- [ ] **Step 1: 删除脚手架冒烟测试（债4）**

删除整个文件 `tests/Quoridor.Domain.Tests/SmokeTests.cs`（`TestHarnessRuns` 2+2==4 脚手架残留，Application.Tests 已有等价 `ScaffoldTests`）。

- [ ] **Step 2: 增强 RuleEngineTests 断言（债5）**

在 `tests/Quoridor.Domain.Tests/Rules/RuleEngineTests.cs` 中：

把 `Illegal_step_is_rejected_state_unchanged` 的断言：

```csharp
        Assert.Contains(r.Events, e => e is MoveRejected);
```

改为：

```csharp
        Assert.Contains(r.Events, e => e is MoveRejected { Reason: RejectReason.IllegalMove });
```

把 `Legal_wall_consumes_one_wall_and_passes_turn` 的断言段（在 `Assert.Contains(r.Events, e => e is WallPlaced);` 之后）追加：

```csharp
        Assert.Contains(r.Events, e => e is TurnPassed);
```

- [ ] **Step 3: 给封死测试加注释（债5）**

在 `tests/Quoridor.Domain.Tests/Path/ReachabilityTests.cs` 的 `Wall_sealing_p1_off_returns_false` 方法注释处，把：

```csharp
        // 在 P1 起点行(row0)上方沿整行水平墙封死，堵死 P1 北上
```

改为：

```csharp
        // 在 P1 起点行(row0)上方沿整行水平墙封死，堵死 P1 北上。
        // 注意：9 列奇数 + 墙长 2 → 无法用不重叠水平墙封死整行 row0；此处用 8 面重叠水平墙
        // 构造封死 fixture（真实对局中同朝向相邻 anchor 共享边=重叠非法），仅用于测试
        // Reachability 返回 false，不代表合法局面。
```

- [ ] **Step 4: 运行测试，验证通过**

Run: `dotnet test tests/Quoridor.Domain.Tests`
Expected: PASS（68 - 1 删除脚手架 = 67）。所有断言增强后仍通过。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "test(domain): 删脚手架冒烟+增强规则断言+封死测试注释(债4,5)"
```

---

## Task 9: Application 集成测试 + README（覆盖债8 Medium 限步）

**Files:**
- Create: `tests/Quoridor.Application.Tests/Integration/GameSessionIntegrationTests.cs`
- Create: `src/Quoridor.Application/README.md`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.Application.Tests/Integration/GameSessionIntegrationTests.cs`：

```csharp
using System.Collections.Generic;
using Quoridor.Application.Seats;
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Xunit;

namespace Quoridor.Application.Tests.Integration;

public class GameSessionIntegrationTests
{
    [Fact]
    public void Full_ai_vs_ai_standard_easy_terminates_with_winner()
    {
        var seats = new IPlayer[]
        {
            AiPlayerFactory.Create(PlayerId.P1, Difficulty.Easy),
            AiPlayerFactory.Create(PlayerId.P2, Difficulty.Easy),
        };
        var session = new GameSession(BoardConfig.Standard, seats);

        session.Start();

        Assert.True(session.State.IsFinished);
        Assert.NotNull(session.State.Winner);
    }

    [Fact]
    public void Medium_self_play_runs_legal_plies_within_cap()  // 债8: Medium 限步覆盖
    {
        var seats = new IPlayer[]
        {
            AiPlayerFactory.Create(PlayerId.P1, Difficulty.Medium),
            AiPlayerFactory.Create(PlayerId.P2, Difficulty.Medium),
        };
        var session = new GameSession(BoardConfig.Kid, seats);
        var rejections = 0;
        session.EventOccurred += e => { if (e is MoveRejected or WallRejected) rejections++; };

        session.Start(maxPlies: 40);  // 限步, 不强制终局(避免 Minimax depth2 全局过慢)

        Assert.Equal(0, rejections);  // 限步内 AI 永不下非法手
        Assert.True(session.State.Pawns.Length == 2);  // 状态结构完好
    }

    [Fact]
    public void Export_import_roundtrip_rebuilds_final_state()
    {
        var seats = new IPlayer[]
        {
            AiPlayerFactory.Create(PlayerId.P1, Difficulty.Easy),
            AiPlayerFactory.Create(PlayerId.P2, Difficulty.Easy),
        };
        var session = new GameSession(BoardConfig.Kid, seats);
        session.Start();
        Assert.True(session.State.IsFinished);

        var notation = session.Export();
        var replay = new ReplayController(BoardConfig.Kid, 2, notation);
        replay.GoTo(replay.Total);

        Assert.Equal(session.State.PawnOf(PlayerId.P1).Pos, replay.Current.PawnOf(PlayerId.P1).Pos);
        Assert.Equal(session.State.PawnOf(PlayerId.P2).Pos, replay.Current.PawnOf(PlayerId.P2).Pos);
        Assert.Equal(session.State.Winner, replay.Current.Winner);
    }

    [Fact]
    public void Human_vs_ai_full_game_human_autoplays_to_completion()
    {
        // 模拟人类用 Greedy 自动决策, 与 AI 对弈到终局, 验证混合座位通道
        var seats = new IPlayer[]
        {
            new AutoplayHuman(PlayerId.P1, Difficulty.Easy),
            AiPlayerFactory.Create(PlayerId.P2, Difficulty.Easy),
        };
        var session = new GameSession(BoardConfig.Kid, seats);
        var plies = 0;

        while (!session.State.IsFinished && plies < 300)
        {
            if (session.State.ActivePlayer == PlayerId.P1)
            {
                var cmd = ((AutoplayHuman)seats[0]).NextCommand(session.State);
                session.Submit(cmd!);
            }
            plies++;
        }

        Assert.True(session.State.IsFinished, $"混合对弈 {plies} 手未终止");
    }

    /// <summary>测试用"自动决策人类": 走 Submit 通道但内部用 Greedy 选命令, 模拟人类输入。</summary>
    private sealed class AutoplayHuman : IPlayer
    {
        private readonly GreedyAi _ai = new();
        public PlayerId Id { get; }
        public bool IsHuman => true;

        public AutoplayHuman(PlayerId id) => Id = id;

        public IGameCommand? NextCommand(GameState state) =>
            state.IsFinished ? null : _ai.Choose(state, Difficulty.Easy);

        // IPlayer.ProposeMove 不被 GameSession 用于人类座位(人类走 Submit), 此处仅满足接口
        IGameCommand? IPlayer.ProposeMove(GameState state) => null;
    }
}
```

- [ ] **Step 2: 运行测试，验证失败**

Run: `dotnet test tests/Quoridor.Application.Tests`
Expected: FAIL（`Integration` 命名空间/`AutoplayHuman` 引用未编译通过——因 `IPlayer.ProposeMove` 显式实现与 `NextCommand` 并存，首跑应编译通过但 `Full_ai_vs_ai...` 等可能因 `Start(maxPlies)` 重载已存在而通过。若已全绿，属"纯测试任务首跑即绿"正常现象，跳过红色阶段，直接进 Step 3 验证）。

> 说明：本任务为集成测试，被测类型（`GameSession.Start(maxPlies)`、`AiPlayerFactory`、`ReplayController`）已在 Task 3/4/7 就绪，故 TDD 红色可能不出现——符合 carryover 记录的"纯测试任务首跑即绿"。重点在 Step 3 确认全部通过且不超时。

- [ ] **Step 3: 运行测试，验证通过**

Run: `dotnet test tests/Quoridor.Application.Tests`
Expected: PASS（26 + 4 = 30）。`Medium_self_play...` 用 Kid+限步40，应 <10s。若 `Full_ai_vs_ai_standard_easy` 超时（Greedy 9×9 全局偶有长局），**不改测试迎合**——报 DONE_WITH_CONCERNS 并考虑改用 Kid。

- [ ] **Step 4: 写 Application README**

`src/Quoridor.Application/README.md`：

```markdown
# Quoridor.Application

Quoridor 对局编排层（纯 C# 类库，零 Godot 依赖）。依赖 `Quoridor.Domain`。

## 构建

```
dotnet build
```

## 测试

```
dotnet test
```

## 职责

- `Logging`：轻量 `IAppLogger` + `NullAppLogger`（默认空实现，UI/宿主注入真实实现）。
- `Seats`：`IPlayer` 座位门槛（人/AI 统一契约）；`HumanPlayer`、`AiPlayer`（包装 `IQuoridorAi`+`Difficulty`）、`AiPlayerFactory`（难度→AI 映射：Easy=Greedy、Medium/Hard=Minimax）。
- `GameSession`：命令调度 + 事件广播 + AI 自动驱动 + 日志。人手与 AI 走同一 `Submit → RuleEngine.ValidateAndApply → 广播` 通道。
- `PreviewService`：设墙悬浮只读预览（合法性 + 各棋子最短路线/步数）。
- `ReplayController`：记谱导入 + ⏮⬅➡⏭ 步进回放。

## 用法

```
var seats = new IPlayer[]
{
    new HumanPlayer(PlayerId.P1),
    AiPlayerFactory.Create(PlayerId.P2, Difficulty.Medium),
};
var session = new GameSession(BoardConfig.Standard, seats);
session.EventOccurred += e => { /* UI 刷新棋盘/记谱面板 */ };
session.Start();

// 人类走子
session.Submit(new MovePawnCommand(new Cell(4, 1)));
// P2(AI) 由 GameSession 自动驱动, 经 EventOccurred 广播

// 导出记谱
string notation = session.Export();

// 回放
var replay = new ReplayController(BoardConfig.Standard, 2, notation);
replay.GoTo(2);
GameState snap = replay.Current;
```

## 边界

- Application 经 `IGameCommand`/`IGameEvent`/`GameState` 等 Domain 类型与外界交互（事件/命令契约）。
- 日志只在 Application 层落实（Domain 不加日志）。
- AI 自动驱动有 `maxPlies` 安全上限，防失控；终局或轮到人类即停。
```

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "test(app): 添加 GameSession 集成测试与 README(覆盖 Medium 限步)"
```

---

## Self-Review（计划自检结果）

**1. Spec 覆盖：**

- §3 Application 层 `GameSession` → Task 4；`Presenter` → 不单建（偏离说明已记录，GameSession 即中枢）；"Command 调度" → Task 4 `Submit`；"记谱服务" → Task 6/7（NotationService 增强 + ReplayController）；"AI 适配器(AIPlayer 适配 IPlayer 门槛接口)" → Task 3。
- §3 关键约束"AI 与人统一门槛 IPlayer" → Task 3 `IPlayer`；"GameSession 不关心座位类型" → Task 4 `_seats` 字典 + `IsHuman` 分流。
- §6.1 正常一回合数据流（Submit→ValidateAndApply→替换状态→广播→AI 自动 Choose）→ Task 4 `Submit`+`DriveAi`。
- §6.2 辅助模式悬浮预览（PoseWall 临时叠墙→Reachability+PathFinder→PreviewResult）→ Task 5。
- §6.3 记谱导出/导入/回放（事件→Encode，串→Decode，逐条 Apply 步进）→ Task 4 `Export` + Task 7 `ReplayController`。
- §7 错误处理：Domain Rejected 事件经 `EventOccurred` 广播（Task 4）；记谱解析结构化 `NotationParseException`（Task 6 增强精确语义）；AI 异常/非法命令防御（Task 4 `DriveAi` 错误日志+停止）；关键方法出入口日志（Task 2/4）。
- §8.2 Application 单测用假 IPlayer 驱动 GameSession → Task 4/9。
- 技术债：债1→Task 6；债2→Task 6；债4→Task 8；债5→Task 8；债7→Task 3(`AiPlayer.ProposeMove`)+Task 4(`DriveAi` 循环条件);债9→Task 3 `AiPlayerFactory`；债8→Task 9 `Medium_self_play`。债3/6 按决策不动。

**2. 占位符扫描：** 无 TBD/TODO；所有步骤含完整代码与测试。Task 1（脚手架）与 Task 8（纯测试清理）无红色阶段，已加注释说明属正常。

**3. 类型一致性：**

- `IAppLogger.Log(LogLevel, string, params object[])` + `NullAppLogger.Instance`：Task 2 定义，Task 4 `GameSession` 构造默认 `NullAppLogger.Instance` 与 `_logger.Log(...)` 调用一致；Task 4 测试 `CapturingLogger` 实现 `IAppLogger` 一致。
- `IPlayer.Id/IsHuman/ProposeMove(GameState)→IGameCommand?`：Task 3 定义，Task 4 `_seats.TryGetValue`+`seat.IsHuman`+`seat.ProposeMove` 调用一致；Task 9 `AutoplayHuman` 显式实现一致。
- `AiPlayerFactory.Create(PlayerId, Difficulty)→AiPlayer`：Task 3 定义，Task 4/9 测试调用一致。
- `GameSession(BoardConfig, IReadOnlyList<IPlayer>, IAppLogger?)` 构造 + `State`/`EventLog`/`EventOccurred`/`Submit(IGameCommand)→ApplyResult`/`Start(int)`/`Export()`：Task 4 定义，Task 5 不用，Task 7 不用，Task 9 调用 `Start(maxPlies:)`/`Export()`/`Submit` 一致。`Start(int maxPlies=DefaultMaxPlies)` 默认参数 → Task 4 测试 `session.Start()` 与 Task 9 `session.Start(maxPlies: 40)` 均合法。
- `PreviewService.PoseWall(GameState, WallPos)→PreviewResult(bool Legal, RejectReason? Reason, ImmutableArray<RoutePreview> Routes)` + `RoutePreview(PlayerId, int Steps, ImmutableArray<Cell> Path)`：Task 5 定义与测试一致；用 `WallLegality.Validate`（返回 `RejectReason?`）与 `PathFinder.ShortestPath`（返回 `PathResult(int, ImmutableArray<Cell>)`）签名与 Domain 一致。
- `ReplayController(BoardConfig, int, string)` + `Current/Cursor/Total/AtStart/AtEnd/Reset/StepForward/StepBack/GoTo`：Task 7 定义与测试一致；用 `NotationService.Decode(string, BoardConfig)`（Task 6 新增重载）与 `RuleEngine.ValidateAndApply` 一致。
- `NotationService.Decode(string, BoardConfig)` 新重载 + `StripMoveNumber` + `ParseCell(string, BoardConfig?)`：Task 6 定义，Task 7 `ReplayController` 调用 `Decode(notation, cfg)` 一致；原 `Decode(string)` 转发 `DecodeCore(notation, null)` 向后兼容。
- 测试中 `r.State!`（无 `.Value`）——遵守 Plan 1 踩坑；`state with { Walls = state.Walls.Add(...) }` 用 `ImmutableArray.Add` 与 record `with`——Domain 已支持。
- Task 4 `Submit_when_finished_is_noop` 用反射置 `State`（`State` 有 private set）——`GameSession` 实现为 `public GameState State { get; private set; }`，反射 `SetValue` 可写；若反射不便，注释已给 `internal SetStateForTest` 备选。

**4. 性能注意：** Task 9 `Medium_self_play` 用 Kid 7×7 + 限步40 控制时长；`Full_ai_vs_ai_standard_easy` 用 Greedy（Plan 2 已验证自对弈终止），若有长局超时则报 DONE_WITH_CONCERNS。

---

## 执行交接

计划已保存到 `docs/superpowers/plans/2026-07-01-quoridor-application.md`。两种执行方式：

1. **Subagent-Driven（推荐）** —— 每任务派独立子 agent + 两阶段评审（与 Plan 1/2 一致）。
2. **Inline Execution** —— 当前会话批量执行 + 检查点。

选哪种？
