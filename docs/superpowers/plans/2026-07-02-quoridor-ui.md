# Quoridor UI (Plan 4) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 交付 Quoridor Godot 桌面客户端 MVP——StartFrame 配置 + GameView 3D 2.5D 棋盘 + 人机/hot-seat 对局 + 设墙辅助模式悬浮预览。

**Architecture:** 三层单向依赖 UI → Application → Domain。新增纯 C# 库 `Quoridor.UI.Logic`（零 Godot 依赖，承载坐标映射/座位构造/拒绝文案/logger 占位符替换，可单测）。新增 Godot 4.7 mono 项目 `Quoridor.UI`，薄 Node 脚本仅做渲染与输入，子节点在 C# `_Ready` 中程序化构建（避免手写 .tscn 的 uid/instance）。`MainController` 为 Autoload 跨场景持久。

**Tech Stack:** Godot 4.7 stable mono、.NET 10 (net10.0)、C#、xunit。shim `godot-mono`。

**Spec:** `docs/superpowers/specs/2026-07-02-quoridor-ui-design.md`

**约定：** 本计划在 worktree 内执行（`superpowers:using-git-worktrees`）。每个 Task 末尾提交。Phase A 全程 TDD；Phase C Godot 脚本无法单测（依赖 Godot 运行时），验证靠 `dotnet build` + Phase D 手动验收。

---

## 进度与阻塞（2026-07-02 更新）

**执行位置**：worktree `C:/workspace/Quoridor/.worktrees/plan4-quoridor-ui`，分支 `plan4-quoridor-ui`（未合并 master）。

**已完成**：
- Phase A（Task 1-8）✅ — 纯逻辑库 `Quoridor.UI.Logic` + 22 单测，`dotnet test Quoridor.slnx` 全绿（Domain 70 + Application 35 + UI.Logic 22 = 127）。提交 `9297743`/`497c0d4`/`4a16f37`/`aab16ca`/`42ffed9`/`8599dcc`。命名占位符测试在实现时修正了计划 Task 7 的位置语义错误（期望 `a=2 b={Y}` 非 `a={X} b=2`）。
- Task 9（Godot 项目脚手架）✅ — `src/Quoridor.UI/` 建成，`dotnet build` 通过。提交 `3a93ebf`（gitignore `.godot/`）、`dd5244d`（脚手架）。csproj 用 `Sdk="Godot.NET.Sdk/4.7.0"`（带版本号）、无显式 GodotSharp 引用。

**阻塞（Task 10 起的 Phase C 受阻）**：
Godot 4.7 mono runtime 无法加载项目程序集。`dotnet build` 产出完整（dll + deps.json + runtimeconfig + [ScriptPath] + 依赖全到位于 `.godot/mono/temp/bin/Debug/`），但 `godot-mono --headless` 日志只报 `.NET: Failed to load project assembly`（无原因），autoload 报 "does not inherit from Node"。`global_script_class_cache.cfg` 保持 `list=[]`，`.godot/mono/metadata/` 空。

**根因线索**：GodotSharp 4.7.0 NuGet 仅发 `lib/net8.0`，而项目 TFM 是 `net10.0`——疑似 TFM 不匹配致加载失败。CLI 构建路径（`--editor --quit`/`--build-solutions`/GUI 后台 150s）均不触发 MSBuild；Godot 4.7 mono 无 "build on load" 设置，构建只能交互式 Build 按钮。

**续 Plan 4 下一步（新 session）**：
1. 用户安装 Godot skill 后优先用其能力。
2. 试 GUI 编辑器 Build 按钮：`godot-mono --path src/Quoridor.UI --editor` → 点 Build → 看 autoload 错误是否消除。
3. 若仍失败 → 把 `Quoridor.UI.csproj` 的 `<TargetFramework>` 改 `net8.0`（可能 Domain/Application 也需同步降级或用 netstandard2.1 桥接），重试。
4. 解除后继续 Task 10-18（GodotAppLogger → 验收+FF 合并）。Task 10-17 代码已在计划中写全，可直接派 subagent 实现。

详见记忆 `plan4-godot-build-blocker`。

---

## File Structure

```
src/Quoridor.UI.Logic/                 (新增, 纯 C# net10.0)
  Quoridor.UI.Logic.csproj
  GameConfig.cs                        开局配置 POCO + MatchMode/Difficulty 映射
  SlotId.cs                            槽位标识 + Edge 枚举
  BoardLayout.cs                       格↔世界坐标、SlotToWall/WallToSlot、PickableSlots
  SeatsBuilder.cs                      GameConfig → IReadOnlyList<IPlayer> + SeatMap
  SeatMap.cs                           Domain PlayerId ↔ 显示玩家 的映射
  RejectReasonText.cs                  RejectReason → 中文文案
  NamedPlaceholder.cs                  logger 命名占位符替换 ({Name} 按位置填充)
src/Quoridor.UI/                       (新增, Godot 4.7 mono)
  project.godot                        主场景=StartFrame, autoload=MainController
  Quoridor.UI.csproj                   ProjectRef: Application + UI.Logic
  Scripts/
    GodotAppLogger.cs                  IAppLogger 实现, 用 NamedPlaceholder + GD.Print
    MainController.cs                  Autoload: 持有 GameConfig/GameSession, Start/EndSession
    BoardView.cs                       Node3D: 构建格子/槽/棋子, Render(state) 幂等, 输入
    PreviewLayerView.cs                Node3D: ImmediateMesh 路线 + Label3D 步数 + 候选墙
    HudView.cs                         CanvasLayer: TopBar/Notation/WallBudget/Footer+回开始页
    GameViewRoot.cs                    Node3D 根: StartSession+订阅+Start, OnEvent 派发
    StartFrameView.cs                  Control 根: 配置项 + 开始按钮 → ChangeScene
  Scenes/
    StartFrame.tscn                    最小 Control 根 + 脚本
    GameView.tscn                      最小 Node3D 根 + 脚本
  Themes/ClassicTheme.tres
tests/Quoridor.UI.Logic.Tests/         (新增, net10.0 xunit)
  Quoridor.UI.Logic.Tests.csproj
  BoardLayoutTests.cs
  SeatsBuilderTests.cs
  RejectReasonTextTests.cs
  NamedPlaceholderTests.cs
```

---

## Phase A — 纯逻辑库（TDD，零 Godot 依赖）

### Task 1: 脚手架 UI.Logic + 测试项目，加入 slnx

**Files:**
- Create: `src/Quoridor.UI.Logic/Quoridor.UI.Logic.csproj`
- Create: `tests/Quoridor.UI.Logic.Tests/Quoridor.UI.Logic.Tests.csproj`
- Modify: `Quoridor.slnx`

- [ ] **Step 1: 建 UI.Logic csproj**

`src/Quoridor.UI.Logic/Quoridor.UI.Logic.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Quoridor.UI.Logic</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Quoridor.Application\Quoridor.Application.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: 建测试 csproj**

`tests/Quoridor.UI.Logic.Tests/Quoridor.UI.Logic.Tests.csproj`:
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
    <ProjectReference Include="..\..\src\Quoridor.UI.Logic\Quoridor.UI.Logic.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: 加入 slnx**

在 `Quoridor.slnx` 的 `/src/` Folder 加 `<Project Path="src/Quoridor.UI.Logic/Quoridor.UI.Logic.csproj" />`；在 `/tests/` Folder 加 `<Project Path="tests/Quoridor.UI.Logic.Tests/Quoridor.UI.Logic.Tests.csproj" />`。

- [ ] **Step 4: 验证构建**

Run: `dotnet build Quoridor.slnx`
Expected: 成功，0 警告 0 错误（无源文件时空程序集也能 build）。

- [ ] **Step 5: 提交**

```bash
git add src/Quoridor.UI.Logic tests/Quoridor.UI.Logic.Tests Quoridor.slnx
git commit -m "feat(ui.logic): 脚手架纯逻辑库与测试项目"
```

---

### Task 2: GameConfig + MatchMode

**Files:**
- Create: `src/Quoridor.UI.Logic/GameConfig.cs`

- [ ] **Step 1: 写 GameConfig**

`src/Quoridor.UI.Logic/GameConfig.cs`:
```csharp
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;

namespace Quoridor.UI.Logic;

public enum MatchMode { VsAi, HotSeat }

/// <summary>StartFrame → MainController → GameView 的开局契约。先手方通过座位顺序达成(见 SeatsBuilder)。</summary>
public sealed record GameConfig(
    BoardVariant Variant,        // Standard(9x9) | Kid(7x7)
    MatchMode Mode,              // VsAi | HotSeat
    Difficulty AiDifficulty,     // VsAi 时生效; HotSeat 时忽略
    PlayerId FirstMove);         // 先手方 P1 | P2
```

- [ ] **Step 2: 验证构建**

Run: `dotnet build src/Quoridor.UI.Logic`
Expected: 成功。

- [ ] **Step 3: 提交**

```bash
git add src/Quoridor.UI.Logic/GameConfig.cs
git commit -m "feat(ui.logic): GameConfig 开局契约"
```

---

### Task 3: SlotId + BoardLayout.SlotToWall（TDD）

**Files:**
- Create: `src/Quoridor.UI.Logic/SlotId.cs`
- Create: `src/Quoridor.UI.Logic/BoardLayout.cs`
- Test: `tests/Quoridor.UI.Logic.Tests/BoardLayoutTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.UI.Logic.Tests/BoardLayoutTests.cs`:
```csharp
using Quoridor.Domain.Core;
using Quoridor.UI.Logic;
using Xunit;

namespace Quoridor.UI.Logic.Tests;

public class BoardLayoutTests
{
    [Fact]
    public void Vertical_slot_maps_to_vertical_wall_with_anchor_equal_slot_coord()
    {
        var layout = new BoardLayout(BoardConfig.Standard, 1.0f);
        var wall = layout.SlotToWall(new SlotId(SlotEdge.Vertical, 3, 4));
        Assert.Equal(new WallPos(new Cell(3, 4), WallOrient.Vertical), wall);
    }

    [Fact]
    public void Horizontal_slot_maps_to_horizontal_wall_with_anchor_equal_slot_coord()
    {
        var layout = new BoardLayout(BoardConfig.Standard, 1.0f);
        var wall = layout.SlotToWall(new SlotId(SlotEdge.Horizontal, 2, 5));
        Assert.Equal(new WallPos(new Cell(2, 5), WallOrient.Horizontal), wall);
    }

    [Theory]
    [InlineData(8)]   // Standard Size=9, MaxIndex=8; 顶排竖槽 r=8 不可触发
    public void Topmost_vertical_slot_is_not_pickable(int r)
    {
        var layout = new BoardLayout(BoardConfig.Standard, 1.0f);
        Assert.Null(layout.SlotToWall(new SlotId(SlotEdge.Vertical, 0, r)));
    }

    [Fact]
    public void Rightmost_horizontal_slot_is_not_pickable()
    {
        var layout = new BoardLayout(BoardConfig.Standard, 1.0f); // MaxIndex=8
        Assert.Null(layout.SlotToWall(new SlotId(SlotEdge.Horizontal, 8, 0)));
    }

    [Fact]
    public void Kid_7_topmost_vertical_slot_not_pickable()
    {
        var layout = new BoardLayout(BoardConfig.Kid, 1.0f); // MaxIndex=6
        Assert.Null(layout.SlotToWall(new SlotId(SlotEdge.Vertical, 0, 6)));
        Assert.NotNull(layout.SlotToWall(new SlotId(SlotEdge.Vertical, 0, 5)));
    }

    [Fact]
    public void Out_of_bounds_slot_returns_null()
    {
        var layout = new BoardLayout(BoardConfig.Standard, 1.0f);
        Assert.Null(layout.SlotToWall(new SlotId(SlotEdge.Vertical, 8, 0))); // c=8 无凹槽
        Assert.Null(layout.SlotToWall(new SlotId(SlotEdge.Horizontal, 0, 8))); // r=8 无凹槽
    }
}
```

- [ ] **Step 2: 运行测试，确认失败**

Run: `dotnet test tests/Quoridor.UI.Logic.Tests`
Expected: 编译失败（`SlotId`/`BoardLayout`/`SlotEdge` 未定义）。

- [ ] **Step 3: 写最小实现**

`src/Quoridor.UI.Logic/SlotId.cs`:
```csharp
namespace Quoridor.UI.Logic;

public enum SlotEdge { Vertical, Horizontal }

public readonly record struct SlotId(SlotEdge Edge, int Col, int Row);
```

`src/Quoridor.UI.Logic/BoardLayout.cs`:
```csharp
using Quoridor.Domain.Core;

namespace Quoridor.UI.Logic;

/// <summary>棋盘布局与坐标映射。SlotId.Row 采用 Domain 行约定: 0=南/底, 向上递增。
/// 屏幕渲染时 CellToWorld 把 row 翻转为世界 Y。槽坐标与 Domain anchor 同构。</summary>
public sealed class BoardLayout
{
    public BoardConfig Cfg { get; }
    public float CellSize { get; }

    public BoardLayout(BoardConfig cfg, float cellSize)
    {
        Cfg = cfg;
        CellSize = cellSize;
    }

    private int MaxIndex => Cfg.MaxIndex;

    /// <summary>竖向槽(c,r) → WallPos((c,r),Vertical); 横向槽(c,r) → WallPos((c,r),Horizontal)。
    /// 顶排竖槽(r=MaxIndex)/最右列横槽(c=MaxIndex)及越界返回 null。</summary>
    public WallPos? SlotToWall(SlotId slot)
    {
        int max = MaxIndex;
        if (slot.Edge == SlotEdge.Vertical)
        {
            int c = slot.Col, r = slot.Row;
            if (c < 0 || c > max - 1) return null;   // 凹槽在列 c,c+1 之间, 需 c+1 存在
            if (r < 0 || r > max - 1) return null;   // 需 r+1 段存在 → r ≤ max-1
            return new WallPos(new Cell(c, r), WallOrient.Vertical);
        }
        else
        {
            int c = slot.Col, r = slot.Row;
            if (r < 0 || r > max - 1) return null;
            if (c < 0 || c > max - 1) return null;
            return new WallPos(new Cell(c, r), WallOrient.Horizontal);
        }
    }
}
```

- [ ] **Step 4: 运行测试，确认通过**

Run: `dotnet test tests/Quoridor.UI.Logic.Tests`
Expected: 全部 PASS。

- [ ] **Step 5: 提交**

```bash
git add src/Quoridor.UI.Logic/SlotId.cs src/Quoridor.UI.Logic/BoardLayout.cs tests/Quoridor.UI.Logic.Tests/BoardLayoutTests.cs
git commit -m "feat(ui.logic): SlotId 与 SlotToWall 映射(TDD)"
```

---

### Task 4: BoardLayout.WallToSlot + CellToWorld + WorldToCell + PickableSlots（TDD）

**Files:**
- Modify: `src/Quoridor.UI.Logic/BoardLayout.cs`
- Test: `tests/Quoridor.UI.Logic.Tests/BoardLayoutTests.cs`

- [ ] **Step 1: 追加失败测试**

在 `BoardLayoutTests.cs` 末尾追加：
```csharp
    [Fact]
    public void WallToSlot_returns_near_slot_and_roundtrips_with_SlotToWall()
    {
        var layout = new BoardLayout(BoardConfig.Standard, 1.0f);
        var wall = new WallPos(new Cell(3, 4), WallOrient.Vertical);
        var slot = layout.WallToSlot(wall);
        Assert.Equal(new SlotId(SlotEdge.Vertical, 3, 4), slot);
        Assert.Equal(wall, layout.SlotToWall(slot!.Value));
    }

    [Fact]
    public void CellToWorld_and_WorldToCell_roundtrip()
    {
        var layout = new BoardLayout(BoardConfig.Standard, 1.0f);
        var cell = new Cell(2, 5);
        var (x, y, z) = layout.CellToWorld(cell);
        var back = layout.WorldToCell(x, z);
        Assert.Equal(cell, back);
    }

    [Fact]
    public void WorldToCell_out_of_bounds_returns_null()
    {
        var layout = new BoardLayout(BoardConfig.Standard, 1.0f);
        Assert.Null(layout.WorldToCell(-1, 0));
        Assert.Null(layout.WorldToCell(0, 999));
    }

    [Fact]
    public void PickableSlots_count_for_standard()
    {
        var layout = new BoardLayout(BoardConfig.Standard, 1.0f);
        // 竖向: c∈[0,7]×r∈[0,7]=64; 横向同 64
        Assert.Equal(128, layout.PickableSlots().Count());
    }
```
（需 `using System.Linq;` 已由 ImplicitUsings 覆盖。）

- [ ] **Step 2: 运行测试，确认失败**

Run: `dotnet test tests/Quoridor.UI.Logic.Tests`
Expected: FAIL（`WallToSlot`/`CellToWorld`/`WorldToCell`/`PickableSlots` 未定义）。

- [ ] **Step 3: 扩展 BoardLayout**

在 `BoardLayout.cs` 类内追加：
```csharp
    /// <summary>反向: 取墙的近边槽(竖墙取下槽 r, 横墙取左槽 c), 用于预览叠绘定位。</summary>
    public SlotId? WallToSlot(WallPos wall)
    {
        var (anchor, orient) = wall;
        if (anchor.Col < 0 || anchor.Col > MaxIndex - 1 || anchor.Row < 0 || anchor.Row > MaxIndex - 1)
            return null;
        return orient == WallOrient.Vertical
            ? new SlotId(SlotEdge.Vertical, anchor.Col, anchor.Row)
            : new SlotId(SlotEdge.Horizontal, anchor.Col, anchor.Row);
    }

    /// <summary>格中心世界坐标。X=Col, Z=翻转后的行(MaxIndex-Row, 使 row 0 在近端/屏幕下方), Y=0(棋盘表面)。</summary>
    public (float X, float Y, float Z) CellToWorld(Cell c)
    {
        float x = c.Col * CellSize;
        float z = (MaxIndex - c.Row) * CellSize;
        return (x, 0f, z);
    }

    public Cell? WorldToCell(float x, float z)
    {
        int col = (int)MathF.Round(x / CellSize);
        int rowFromBottom = (int)MathF.Round((z) / CellSize);
        int row = MaxIndex - rowFromBottom;
        if (col < 0 || col > MaxIndex || row < 0 || row > MaxIndex) return null;
        return new Cell(col, row);
    }

    public IEnumerable<SlotId> PickableSlots()
    {
        for (int c = 0; c <= MaxIndex - 1; c++)
            for (int r = 0; r <= MaxIndex - 1; r++)
            {
                yield return new SlotId(SlotEdge.Vertical, c, r);
                yield return new SlotId(SlotEdge.Horizontal, c, r);
            }
    }
```
顶部加 `using System.Collections.Generic;`（ImplicitUsings 已含，按需）。

- [ ] **Step 4: 运行测试，确认通过**

Run: `dotnet test tests/Quoridor.UI.Logic.Tests`
Expected: 全部 PASS。

- [ ] **Step 5: 提交**

```bash
git add src/Quoridor.UI.Logic/BoardLayout.cs tests/Quoridor.UI.Logic.Tests/BoardLayoutTests.cs
git commit -m "feat(ui.logic): WallToSlot/CellToWorld/WorldToCell/PickableSlots(TDD)"
```

---

### Task 5: SeatsBuilder + SeatMap（TDD）

**Files:**
- Create: `src/Quoridor.UI.Logic/SeatMap.cs`
- Create: `src/Quoridor.UI.Logic/SeatsBuilder.cs`
- Test: `tests/Quoridor.UI.Logic.Tests/SeatsBuilderTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.UI.Logic.Tests/SeatsBuilderTests.cs`:
```csharp
using Quoridor.Application.Seats;
using Quoridor.Domain.Core;
using Quoridor.UI.Logic;
using Xunit;

namespace Quoridor.UI.Logic.Tests;

public class SeatsBuilderTests
{
    [Fact]
    public void VsAi_first_P1_yields_P1_human_P2_ai()
    {
        var cfg = new GameConfig(BoardVariant.Standard, MatchMode.VsAi, Domain.AI.Difficulty.Medium, PlayerId.P1);
        var seats = SeatsBuilder.Build(cfg);
        Assert.Equal(PlayerId.P1, seats[0].Id);
        Assert.Equal(PlayerId.P2, seats[1].Id);
        Assert.True(seats[0].IsHuman);
        Assert.False(seats[1].IsHuman);
    }

    [Fact]
    public void VsAi_first_P2_swaps_seats_so_P1_is_ai()
    {
        var cfg = new GameConfig(BoardVariant.Standard, MatchMode.VsAi, Domain.AI.Difficulty.Medium, PlayerId.P2);
        var seats = SeatsBuilder.Build(cfg);
        Assert.Equal(PlayerId.P1, seats[0].Id);
        Assert.False(seats[0].IsHuman);   // P1=AI(玩家2身份,先手)
        Assert.True(seats[1].IsHuman);    // P2=人类(玩家1身份,后手)
    }

    [Fact]
    public void HotSeat_both_human_regardless_of_first()
    {
        var cfgP1 = new GameConfig(BoardVariant.Kid, MatchMode.HotSeat, Domain.AI.Difficulty.Easy, PlayerId.P1);
        var cfgP2 = new GameConfig(BoardVariant.Kid, MatchMode.HotSeat, Domain.AI.Difficulty.Easy, PlayerId.P2);
        foreach (var cfg in new[] { cfgP1, cfgP2 })
        {
            var seats = SeatsBuilder.Build(cfg);
            Assert.True(seats.All(s => s.IsHuman));
            Assert.Equal(2, seats.Count);
        }
    }

    [Fact]
    public void SeatMap_first_P1_maps_P1_to_player1()
    {
        var map = SeatMap.ForFirstMove(PlayerId.P1);
        Assert.Equal(1, map.ToDisplayNumber(PlayerId.P1));
        Assert.Equal(2, map.ToDisplayNumber(PlayerId.P2));
        Assert.Equal(PlayerId.P1, map.FromDisplayNumber(1));
    }

    [Fact]
    public void SeatMap_first_P2_swaps_display_numbers()
    {
        var map = SeatMap.ForFirstMove(PlayerId.P2);
        Assert.Equal(2, map.ToDisplayNumber(PlayerId.P1));  // P1 座位=玩家2
        Assert.Equal(1, map.ToDisplayNumber(PlayerId.P2));  // P2 座位=玩家1
        Assert.Equal(PlayerId.P2, map.FromDisplayNumber(1));
    }
}
```

- [ ] **Step 2: 运行测试，确认失败**

Run: `dotnet test tests/Quoridor.UI.Logic.Tests`
Expected: FAIL（`SeatsBuilder`/`SeatMap` 未定义）。

- [ ] **Step 3: 写实现**

`src/Quoridor.UI.Logic/SeatMap.cs`:
```csharp
using Quoridor.Domain.Core;

namespace Quoridor.UI.Logic;

/// <summary>Domain PlayerId ↔ 显示玩家编号 的映射。先手=P2 时 P1 座位=玩家2, P2 座位=玩家1。</summary>
public readonly record struct SeatMap(PlayerId FirstMove)
{
    public static SeatMap ForFirstMove(PlayerId first) => new(first);

    public int ToDisplayNumber(PlayerId id) =>
        FirstMove == PlayerId.P1
            ? (id == PlayerId.P1 ? 1 : 2)
            : (id == PlayerId.P1 ? 2 : 1);

    public PlayerId FromDisplayNumber(int display) =>
        FirstMove == PlayerId.P1
            ? (display == 1 ? PlayerId.P1 : PlayerId.P2)
            : (display == 1 ? PlayerId.P2 : PlayerId.P1);
}
```

`src/Quoridor.UI.Logic/SeatsBuilder.cs`:
```csharp
using Quoridor.Application.Seats;
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;

namespace Quoridor.UI.Logic;

public static class SeatsBuilder
{
    /// <summary>返回 [P1 座位, P2 座位]。P1 始终是 Domain 起手(GameSetup 硬编码)。
    /// 先手=P1: P1=玩家1, P2=玩家2; 先手=P2: P1=玩家2(先手), P2=玩家1(后手)。
    /// VsAi: 人类=玩家1, AI=玩家2(固定身份); 故先手=P2 时 P1 座位=AI, P2 座位=人类。</summary>
    public static IReadOnlyList<IPlayer> Build(GameConfig cfg)
    {
        bool firstIsHuman = cfg.Mode == MatchMode.HotSeat || cfg.FirstMove == PlayerId.P1;
        IPlayer firstSeat = MakeSeat(PlayerId.P1, cfg, firstIsHuman);
        IPlayer secondSeat = MakeSeat(PlayerId.P2, cfg, !firstIsHuman && cfg.Mode == MatchMode.VsAi ? false : true);
        // HotSeat 下双方都人; VsAi 下人类=玩家1、AI=玩家2:
        // 先手=P1: P1(人),P2(AI); 先手=P2: P1(AI,玩家2先手),P2(人)
        if (cfg.Mode == MatchMode.HotSeat)
        {
            firstSeat = new HumanPlayer(PlayerId.P1);
            secondSeat = new HumanPlayer(PlayerId.P2);
        }
        else
        {
            if (cfg.FirstMove == PlayerId.P1)
            {
                firstSeat = new HumanPlayer(PlayerId.P1);
                secondSeat = AiPlayerFactory.Create(PlayerId.P2, cfg.AiDifficulty);
            }
            else
            {
                firstSeat = AiPlayerFactory.Create(PlayerId.P1, cfg.AiDifficulty);
                secondSeat = new HumanPlayer(PlayerId.P2);
            }
        }
        return new[] { firstSeat, secondSeat };
    }

    private static IPlayer MakeSeat(PlayerId id, GameConfig cfg, bool isHuman) =>
        isHuman ? new HumanPlayer(id) : AiPlayerFactory.Create(id, cfg.AiDifficulty);
}
```
（`MakeSeat` 在 HotSeat/VsAi 分支后未使用，仅为清晰；若编译器警告未使用，删除 `MakeSeat` 私有方法及两行赋值，保留 if/else 分支即可——见 Step 4 验证。）

- [ ] **Step 4: 运行测试 + 处理未使用代码**

Run: `dotnet build src/Quoridor.UI.Logic`
若报 `MakeSeat` 未使用警告，删除 `MakeSeat` 方法与 `firstSeat`/`secondSeat` 初始两行赋值，仅保留 `if (cfg.Mode == MatchMode.HotSeat) {...} else {...}` 块。
Run: `dotnet test tests/Quoridor.UI.Logic.Tests`
Expected: 全部 PASS。

- [ ] **Step 5: 提交**

```bash
git add src/Quoridor.UI.Logic/SeatMap.cs src/Quoridor.UI.Logic/SeatsBuilder.cs tests/Quoridor.UI.Logic.Tests/SeatsBuilderTests.cs
git commit -m "feat(ui.logic): SeatsBuilder 座位换位 + SeatMap 映射(TDD)"
```

---

### Task 6: RejectReasonText（TDD）

**Files:**
- Create: `src/Quoridor.UI.Logic/RejectReasonText.cs`
- Test: `tests/Quoridor.UI.Logic.Tests/RejectReasonTextTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.UI.Logic.Tests/RejectReasonTextTests.cs`:
```csharp
using Quoridor.Domain.Core;
using Quoridor.UI.Logic;
using Xunit;

namespace Quoridor.UI.Logic.Tests;

public class RejectReasonTextTests
{
    [Fact]
    public void Every_reason_has_nonempty_text()
    {
        foreach (RejectReason r in Enum.GetValues(typeof(RejectReason)))
            Assert.False(string.IsNullOrWhiteSpace(RejectReasonText.Of(r)));
    }

    [Fact]
    public void NoWallsLeft_has_meaningful_text()
    {
        Assert.Contains("墙", RejectReasonText.Of(RejectReason.NoWallsLeft));
    }
}
```

- [ ] **Step 2: 运行测试，确认失败**

Run: `dotnet test tests/Quoridor.UI.Logic.Tests`
Expected: FAIL（`RejectReasonText` 未定义）。

- [ ] **Step 3: 写实现**

`src/Quoridor.UI.Logic/RejectReasonText.cs`:
```csharp
using Quoridor.Domain.Core;

namespace Quoridor.UI.Logic;

public static class RejectReasonText
{
    public static string Of(RejectReason r) => r switch
    {
        RejectReason.NotYourTurn => "还没轮到你",
        RejectReason.IllegalMove => "非法走子",
        RejectReason.BlockedByWall => "被墙挡住",
        RejectReason.OffBoard => "越出棋盘",
        RejectReason.WallOverlap => "墙位重叠",
        RejectReason.WallPlusIntersection => "墙与十字交叉冲突",
        RejectReason.WallOutOfBounds => "墙越界",
        RejectReason.WallBlocksAllPaths => "此墙会封死某方路径",
        RejectReason.NoWallsLeft => "墙已用完",
        RejectReason.GameFinished => "对局已结束",
        _ => r.ToString(),
    };
}
```

- [ ] **Step 4: 运行测试，确认通过**

Run: `dotnet test tests/Quoridor.UI.Logic.Tests`
Expected: 全部 PASS。

- [ ] **Step 5: 提交**

```bash
git add src/Quoridor.UI.Logic/RejectReasonText.cs tests/Quoridor.UI.Logic.Tests/RejectReasonTextTests.cs
git commit -m "feat(ui.logic): RejectReason 中文文案(TDD)"
```

---

### Task 7: NamedPlaceholder logger 占位符替换（TDD）

**Files:**
- Create: `src/Quoridor.UI.Logic/NamedPlaceholder.cs`
- Test: `tests/Quoridor.UI.Logic.Tests/NamedPlaceholderTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/Quoridor.UI.Logic.Tests/NamedPlaceholderTests.cs`:
```csharp
using Quoridor.UI.Logic;
using Xunit;

namespace Quoridor.UI.Logic.Tests;

public class NamedPlaceholderTests
{
    [Fact]
    public void Named_tokens_filled_by_position()
    {
        Assert.Equal("cfg=K players=2", NamedPlaceholder.Format("cfg={Variant} players={N}", "K", 2));
    }

    [Fact]
    public void Escaped_braces_literal()
    {
        Assert.Equal("{x}", NamedPlaceholder.Format("{{x}}", 1));
    }

    [Fact]
    public void More_placeholders_than_args_keeps_remaining_token()
    {
        // 参数不足时降级: 保留原 token, 不抛
        Assert.Equal("a={X} b=2", NamedPlaceholder.Format("a={X} b={Y}", 2));
    }

    [Fact]
    public void More_args_than_placeholders_ignores_extra()
    {
        Assert.Equal("only=1", NamedPlaceholder.Format("only={A}", 1, 2, 3));
    }

    [Fact]
    public void Empty_message_no_args()
    {
        Assert.Equal("", NamedPlaceholder.Format(""));
    }
}
```

- [ ] **Step 2: 运行测试，确认失败**

Run: `dotnet test tests/Quoridor.UI.Logic.Tests`
Expected: FAIL（`NamedPlaceholder` 未定义）。

- [ ] **Step 3: 写实现**

`src/Quoridor.UI.Logic/NamedPlaceholder.cs`:
```csharp
using System.Text;
using System.Text.RegularExpressions;

namespace Quoridor.UI.Logic;

/// <summary>把 Application 日志的命名占位符 {Name} 按 args 顺序填充。不可用 string.Format(命名 token 会抛)。</summary>
public static class NamedPlaceholder
{
    private static readonly Regex Token = new(@"\{(?<name>[^{}]+)\}", RegexOptions.Compiled);

    public static string Format(string message, params object[] args)
    {
        if (string.IsNullOrEmpty(message)) return message ?? string.Empty;
        var sb = new StringBuilder();
        int idx = 0;
        int i = 0;
        while (i < message.Length)
        {
            if (message[i] == '{' && i + 1 < message.Length && message[i + 1] == '{')
            { sb.Append('{'); i += 2; continue; }
            if (message[i] == '}' && i + 1 < message.Length && message[i + 1] == '}')
            { sb.Append('}'); i += 2; continue; }
            if (message[i] == '{')
            {
                int close = message.IndexOf('}', i + 1);
                if (close > i)
                {
                    if (idx < args.Length)
                    { sb.Append(args[idx]?.ToString() ?? ""); idx++; }
                    else
                    { sb.Append(message, i, close - i + 1); }  // 保留原 token
                    i = close + 1;
                    continue;
                }
            }
            sb.Append(message[i]); i++;
        }
        return sb.ToString();
    }
}
```

- [ ] **Step 4: 运行测试，确认通过**

Run: `dotnet test tests/Quoridor.UI.Logic.Tests`
Expected: 全部 PASS。

- [ ] **Step 5: 提交**

```bash
git add src/Quoridor.UI.Logic/NamedPlaceholder.cs tests/Quoridor.UI.Logic.Tests/NamedPlaceholderTests.cs
git commit -m "feat(ui.logic): NamedPlaceholder 命名占位符替换(TDD)"
```

---

### Task 8: Phase A 全量回归

- [ ] **Step 1: 跑全部测试**

Run: `dotnet test Quoridor.slnx`
Expected: Domain + Application + UI.Logic 全绿，新增 UI.Logic 测试 ≥ 15 条。

- [ ] **Step 2: 提交（无变更则跳过）**

若有回归修复则提交；否则无操作。

---

## Phase B — Godot 项目脚手架

### Task 9: 创建 Godot 项目与 csproj，加入 slnx

**Files:**
- Create: `src/Quoridor.UI/project.godot`
- Create: `src/Quoridor.UI/Quoridor.UI.csproj`
- Modify: `Quoridor.slnx`

- [ ] **Step 1: 建 csproj**

`src/Quoridor.UI/Quoridor.UI.csproj`:
```xml
<Project Sdk="Godot.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <RootNamespace>Quoridor.UI</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="GodotSharp" Version="4.4.0" />  <!-- Godot 4.7 mono 兼容; 若 build 报版本不匹配改 4.4.0-rc/4.5 -->
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Quoridor.Application\Quoridor.Application.csproj" />
    <ProjectReference Include="..\Quoridor.UI.Logic\Quoridor.UI.Logic.csproj" />
  </ItemGroup>
</Project>
```

> 注：`Godot.NET.Sdk` 的 GodotSharp 版本须与本机 Godot 4.7 mono 匹配。若 `dotnet build` 报版本冲突，运行 `godot-mono --headless --quit --path src/Quoridor.UI` 让 Godot 重写 csproj 的 GodotSharp 版本，或手动改为 `4.4.0`。本步 Step 4 验证。

- [ ] **Step 2: 建 project.godot**

`src/Quoridor.UI/project.godot`:
```ini
config_version=5

[application]

config/name="Quoridor"
run/main_scene="res://Scenes/StartFrame.tscn"
config/features=4.7
config/icon="res://icon.svg"

[autoload]

MainController="*res://Scripts/MainController.cs"

[display]

window/size/viewport_width=1280
window/size/viewport_height=800

[input]

[rendering]

renderer/rendering_method="forward_plus"
```

- [ ] **Step 3: 加入 slnx + 占位脚本**

在 `Quoridor.slnx` 的 `/src/` Folder 加：
```xml
    <Project Path="src/Quoridor.UI/Quoridor.UI.csproj" />
```

先建占位 `Scripts/MainController.cs`（否则 autoload 引用悬空）：
```csharp
using Godot;

namespace Quoridor.UI;

public partial class MainController : Node
{
    public override void _Ready() { }
}
```
建空 `Scenes/` 目录与占位 `Scenes/StartFrame.tscn`:
```
[gd_scene format=3]
[node name="StartFrame" type="Control"]
```
建占位 `icon.svg`（任一 1x1 svg）或删除 `config/icon` 行。

- [ ] **Step 4: 验证 Godot 加载与 build**

Run: `godot-mono --headless --quit --path src/Quoridor.UI`
Expected: 无报错退出（Godot 生成 .godot/ 缓存与 .csproj 的 GodotSharp 版本对齐）。

Run: `dotnet build src/Quoridor.UI/Quoridor.UI.csproj`
Expected: 成功。若 GodotSharp 版本不匹配，按 Step 1 注释调整版本号后重试。

- [ ] **Step 5: 提交**

```bash
git add src/Quoridor.UI Quoridor.slnx
git commit -m "feat(ui): Godot 项目脚手架 + MainController autoload 占位"
```

---

## Phase C — Godot 脚本与场景

> 本 Phase 脚本依赖 Godot 运行时，无法 xunit 单测。每个 Task 验证靠 `dotnet build`；端到端验证在 Phase D。

### Task 10: GodotAppLogger

**Files:**
- Create: `src/Quoridor.UI/Scripts/GodotAppLogger.cs`

- [ ] **Step 1: 写实现**

`src/Quoridor.UI/Scripts/GodotAppLogger.cs`:
```csharp
using System.Collections.Generic;
using Godot;
using Quoridor.Application.Logging;
using Quoridor.UI.Logic;

namespace Quoridor.UI;

/// <summary>把 Application 的 IAppLogger 桥到 Godot: 命名占位符替换后 GD.Print/PushWarning/PushError。
/// 同时写内存 ring buffer 供手动验收事后核查(见 spec §9.2)。</summary>
public sealed class GodotAppLogger : IAppLogger
{
    private readonly Queue<string> _warnings = new();
    private const int BufferCap = 256;

    public IReadOnlyCollection<string> RecentWarnings => _warnings;

    public void Log(LogLevel level, string message, params object[] args)
    {
        string formatted = NamedPlaceholder.Format(message, args);
        switch (level)
        {
            case LogLevel.Debug:
            case LogLevel.Info:
                GD.Print(formatted);
                break;
            case LogLevel.Warning:
                GD.PushWarning(formatted);
                EnqueueWarning(formatted);
                break;
            case LogLevel.Error:
                GD.PushError(formatted);
                EnqueueWarning(formatted);
                break;
        }
    }

    private void EnqueueWarning(string s)
    {
        _warnings.Enqueue(s);
        while (_warnings.Count > BufferCap) _warnings.Dequeue();
    }
}
```

- [ ] **Step 2: 验证构建**

Run: `dotnet build src/Quoridor.UI`
Expected: 成功。

- [ ] **Step 3: 提交**

```bash
git add src/Quoridor.UI/Scripts/GodotAppLogger.cs
git commit -m "feat(ui): GodotAppLogger 桥接 IAppLogger 到 GD.Print"
```

---

### Task 11: MainController autoload

**Files:**
- Modify: `src/Quoridor.UI/Scripts/MainController.cs`（替换占位）

- [ ] **Step 1: 写实现**

`src/Quoridor.UI/Scripts/MainController.cs`:
```csharp
using Godot;
using Quoridor.Application;
using Quoridor.Domain.Core;
using Quoridor.UI.Logic;

namespace Quoridor.UI;

/// <summary>Autoload: 跨场景持久。持有 GameConfig 与 GameSession(仅 GameView 在场期间)。
/// StartFrame 写 Config; GameView._Ready 调 StartSession 构造并订阅, _ExitTree 调 EndSession。</summary>
public partial class MainController : Node
{
    public GameConfig? Config { get; set; }
    public GameSession? Session { get; private set; }
    public GodotAppLogger Logger { get; } = new();

    public BoardConfig BoardConfig =>
        Config?.Variant == BoardVariant.Kid ? BoardConfig.Kid : BoardConfig.Standard;

    public void StartSession(GameConfig cfg)
    {
        Config = cfg;
        EndSession();
        var seats = SeatsBuilder.Build(cfg);
        var board = cfg.Variant == BoardVariant.Kid ? BoardConfig.Kid : BoardConfig.Standard;
        Session = new GameSession(board, seats, Logger);
        GD.Print($"MainController.StartSession variant={cfg.Variant} mode={cfg.Mode} first={cfg.FirstMove}");
    }

    public void EndSession()
    {
        if (Session is not null)
        {
            GD.Print("MainController.EndSession");
            Session = null;
        }
    }

    public override void _Ready() { }
}
```

- [ ] **Step 2: 验证构建**

Run: `dotnet build src/Quoridor.UI`
Expected: 成功。

- [ ] **Step 3: 提交**

```bash
git add src/Quoridor.UI/Scripts/MainController.cs
git commit -m "feat(ui): MainController 持有 GameSession 与配置"
```

---

### Task 12: BoardView（构建棋盘 + 幂等 Render + 输入）

**Files:**
- Create: `src/Quoridor.UI/Scripts/BoardView.cs`

- [ ] **Step 1: 写实现**

`src/Quoridor.UI/Scripts/BoardView.cs`:
```csharp
using System.Collections.Generic;
using Godot;
using Quoridor.Application;
using Quoridor.Domain.Core;
using Quoridor.UI.Logic;

namespace Quoridor.UI;

/// <summary>3D 棋盘渲染与输入。程序化构建格子/槽/棋子; Render(state) 幂等全量刷新。
/// 输入: Cell 点击走子; WallSlot hover 预览 + 点击设墙。结构变更 CallDeferred。</summary>
public partial class BoardView : Node3D
{
    private MainController? _ctrl;
    private BoardLayout? _layout;
    private readonly Dictionary<Cell, Area3D> _cells = new();
    private readonly Dictionary<SlotId, Area3D> _slots = new();
    private readonly Dictionary<PlayerId, MeshInstance3D> _pawns = new();
    private readonly Dictionary<WallPos, MeshInstance3D> _walls = new();

    private StandardMaterial3D _boardMat = new() { AlbedoColor = new Color(0.85f, 0.78f, 0.6f), Roughness = 0.6f };
    private StandardMaterial3D _wallMat = new() { AlbedoColor = new Color(0.45f, 0.3f, 0.18f), Roughness = 0.5f };
    private StandardMaterial3D _pawn1Mat = new() { AlbedoColor = new Color(0.9f, 0.85f, 0.2f), Roughness = 0.3f, Metallic = 0.2f };
    private StandardMaterial3D _pawn2Mat = new() { AlbedoColor = new Color(0.2f, 0.5f, 0.9f), Roughness = 0.3f, Metallic = 0.2f };

    public BoardLayout Layout => _layout!;
    public event Action<Cell>? CellClicked;
    public event Action<SlotId>? SlotHovered;
    public event Action<SlotId>? SlotClicked;
    public event Action? SlotCleared;

    public void Init(MainController ctrl)
    {
        _ctrl = ctrl;
        var board = ctrl.BoardConfig;
        _layout = new BoardLayout(board, 1.0f);
        BuildBoard(board);
        BuildPawns(ctrl.Session!.State);
    }

    private void BuildBoard(BoardConfig board)
    {
        // 棋盘平板
        var plate = new MeshInstance3D();
        plate.Mesh = new BoxShape3D(); // 仅为占位; 用 PlaneMesh 更合适:
        plate.Mesh = new PlaneMesh { Size = new Vector2(board.Size * Layout.CellSize, board.Size * Layout.CellSize) };
        plate.MaterialOverride = _boardMat;
        plate.Position = new Vector3((board.MaxIndex) * Layout.CellSize / 2f, 0, (board.MaxIndex) * Layout.CellSize / 2f);
        AddChild(plate);

        // 格子点击区
        for (int r = 0; r <= board.MaxIndex; r++)
            for (int c = 0; c <= board.MaxIndex; c++)
            {
                var cell = new Cell(c, r);
                var area = MakePickArea(Layout.CellToWorld(cell), new Vector3(Layout.CellSize, 0.02f, Layout.CellSize));
                area.InputEvent += (cam, ev, pos, normal, shape) => OnCellInput(ev, cell);
                _cells[cell] = area;
                AddChild(area);
            }

        // 槽位(只建可触发的)
        foreach (var slot in Layout.PickableSlots())
        {
            var wall = Layout.SlotToWall(slot)!.Value;
            var area = MakeSlotArea(slot, wall);
            area.MouseEntered += () => SlotHovered?.Invoke(slot);
            area.MouseExited += () => SlotCleared?.Invoke();
            area.InputEvent += (cam, ev, pos, normal, shape) => OnSlotInput(ev, slot);
            _slots[slot] = area;
            AddChild(area);
        }
    }

    private Area3D MakePickArea((float X, float Y, float Z) pos, Vector3 size)
    {
        var area = new Area3D();
        var col = new CollisionShape3D();
        var box = new BoxShape3D { Size = size };
        col.Shape = box;
        area.AddChild(col);
        area.Position = new Vector3(pos.X, pos.Y, pos.Z);
        area.InputRayPickable = true;
        return area;
    }

    private Area3D MakeSlotArea(SlotId slot, WallPos wall)
    {
        // 槽的拾取区放在墙将出现的位置中点
        var anchor = wall.Anchor;
        float cx = (anchor.Col + 0.5f) * Layout.CellSize;
        float cz = (Layout.Cfg.MaxIndex - (anchor.Row + 0.5f)) * Layout.CellSize;
        Vector3 size = slot.Edge == SlotEdge.Vertical
            ? new Vector3(0.12f, 0.4f, Layout.CellSize * 2f)
            : new Vector3(Layout.CellSize * 2f, 0.4f, 0.12f);
        return MakePickArea((cx, 0.2f, cz), size);
    }

    private void BuildPawns(GameState state)
    {
        foreach (var pawn in state.Pawns)
        {
            var mesh = new MeshInstance3D();
            mesh.Mesh = new CylinderMesh { Radius = 0.25f, Height = 0.5f };
            mesh.MaterialOverride = pawn.Owner == PlayerId.P1 ? _pawn1Mat : _pawn2Mat;
            AddChild(mesh);
            _pawns[pawn.Owner] = mesh;
        }
        Render(state);
    }

    /// <summary>幂等全量刷新。基于最新 State 重算棋子位置/墙集合/输入开关。</summary>
    public void Render(GameState state)
    {
        // 棋子位置
        foreach (var pawn in state.Pawns)
        {
            if (_pawns.TryGetValue(pawn.Owner, out var m))
            {
                var (x, y, z) = Layout.CellToWorld(pawn.Pos);
                m.Position = new Vector3(x, 0.25f, z);
            }
        }
        // 墙集合对齐(缺的补, 多的删) — 结构变更 CallDeferred
        var desired = new HashSet<WallPos>(state.Walls);
        CallDeferred(nameof(SyncWalls), desired);
        // 墙数=0 禁用槽拾取
        bool wallable = state.PlayerOf(state.ActivePlayer).WallsLeft > 0 && !state.IsFinished;
        foreach (var kv in _slots) kv.Value.InputRayPickable = wallable;
        // 终局禁用格子
        foreach (var kv in _cells) kv.Value.InputRayPickable = !state.IsFinished;
    }

    private void SyncWalls(HashSet<WallPos> desired)
    {
        // 删除多余的
        var toRemove = new List<WallPos>();
        foreach (var kv in _walls) if (!desired.Contains(kv.Key)) toRemove.Add(kv.Key);
        foreach (var w in toRemove) { _walls[w].QueueFree(); _walls.Remove(w); }
        // 补齐缺失的
        foreach (var w in desired)
        {
            if (_walls.ContainsKey(w)) continue;
            var mesh = new MeshInstance3D();
            mesh.Mesh = new BoxMesh();
            var anchor = w.Anchor;
            float cx = (anchor.Col + 0.5f) * Layout.CellSize;
            float cz = (Layout.Cfg.MaxIndex - (anchor.Row + 0.5f)) * Layout.CellSize;
            bool vertical = w.Orient == WallOrient.Vertical;
            mesh.Scale = new Vector3(vertical ? 0.1f : Layout.CellSize * 2f, 0.6f, vertical ? Layout.CellSize * 2f : 0.1f);
            mesh.Position = new Vector3(cx, 0.3f, cz);
            mesh.MaterialOverride = _wallMat;
            AddChild(mesh);
            _walls[w] = mesh;
        }
    }

    private void OnCellInput(InputEvent ev, Cell cell)
    {
        if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
            CellClicked?.Invoke(cell);
    }

    private void OnSlotInput(InputEvent ev, SlotId slot)
    {
        if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
            SlotClicked?.Invoke(slot);
    }
}
```

- [ ] **Step 2: 验证构建**

Run: `dotnet build src/Quoridor.UI`
Expected: 成功。若有 `using` 缺失按编译器提示补。

- [ ] **Step 3: 提交**

```bash
git add src/Quoridor.UI/Scripts/BoardView.cs
git commit -m "feat(ui): BoardView 程序化棋盘 + 幂等 Render + 输入"
```

---

### Task 13: PreviewLayerView（悬浮预览叠绘）

**Files:**
- Create: `src/Quoridor.UI/Scripts/PreviewLayerView.cs`

- [ ] **Step 1: 写实现**

`src/Quoridor.UI/Scripts/PreviewLayerView.cs`:
```csharpusing System.Collections.Generic;
using Godot;
using Quoridor.Application;
using Quoridor.Domain.Core;
using Quoridor.UI.Logic;

namespace Quoridor.UI;

/// <summary>悬浮预览: 候选墙半透明立柱(合法绿/非法红) + 各棋子最短路线(ImmediateMesh) + 步数(Label3D)。
/// Show(preview) 全量重建; Clear() 清空。</summary>
public partial class PreviewLayerView : Node3D
{
    private BoardLayout? _layout;
    private ImmediateMesh? _routeMesh;
    private MeshInstance3D? _routeInst;
    private readonly List<Label3D> _stepLabels = new();
    private MeshInstance3D? _candidateWall;

    private static readonly StandardMaterial3D _lineMat = new()
    { AlbedoColor = new Color(0.2f, 0.9f, 0.3f, 0.9f), NoDepthTest = true };
    private static readonly StandardMaterial3D _legalMat = new()
    { AlbedoColor = new Color(0.2f, 0.9f, 0.3f, 0.4f), Transparency = BaseMaterial3D.TransparencyModeEnum.Alpha };
    private static readonly StandardMaterial3D _illegalMat = new()
    { AlbedoColor = new Color(0.9f, 0.2f, 0.2f, 0.4f), Transparency = BaseMaterial3D.TransparencyModeEnum.Alpha };

    public void Init(BoardLayout layout)
    {
        _layout = layout;
        _routeInst = new MeshInstance3D();
        _routeMesh = new ImmediateMesh();
        _routeInst.Mesh = _routeMesh;
        _routeInst.MaterialOverride = _lineMat;
        AddChild(_routeInst);
    }

    public void Show(PreviewResult preview, WallPos wall)
    {
        Clear();
        // 候选墙
        _candidateWall = new MeshInstance3D { Mesh = new BoxMesh() };
        var anchor = wall.Anchor;
        float cx = (anchor.Col + 0.5f) * _layout!.CellSize;
        float cz = (_layout.Cfg.MaxIndex - (anchor.Row + 0.5f)) * _layout.CellSize;
        bool vertical = wall.Orient == WallOrient.Vertical;
        _candidateWall.Scale = new Vector3(vertical ? 0.12f : _layout.CellSize * 2f, 0.6f, vertical ? _layout.CellSize * 2f : 0.12f);
        _candidateWall.Position = new Vector3(cx, 0.3f, cz);
        _candidateWall.MaterialOverride = preview.Legal ? _legalMat : _illegalMat;
        AddChild(_candidateWall);

        if (!preview.Legal) return;

        // 路线 + 步数
        _routeMesh!.ClearSurfaces();
        _routeMesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);
        foreach (var route in preview.Routes)
        {
            foreach (var cell in route.Path)
            {
                var (x, _, z) = _layout.CellToWorld(cell);
                _routeMesh.SurfaceAddVertex(new Vector3(x, 0.05f, z));
            }
            var label = new Label3D { Text = $"{route.Steps}", Position = _layout.CellToWorld(route.Path[0]) with { Y = 0.6f } };
            label.FontSize = 32;
            AddChild(label);
            _stepLabels.Add(label);
        }
        _routeMesh.SurfaceEnd();
    }

    public void Clear()
    {
        if (_candidateWall is not null) { _candidateWall.QueueFree(); _candidateWall = null; }
        foreach (var l in _stepLabels) l.QueueFree();
        _stepLabels.Clear();
        _routeMesh?.ClearSurfaces();
    }
}
```
（首行 `using System.Collections.Generic;` 前误植一个 `using` 连写，确保文件首行为 `using System.Collections.Generic;`——见 Step 2 验证。）

- [ ] **Step 2: 验证构建**

Run: `dotnet build src/Quoridor.UI`
Expected: 成功。若 `PreviewResult` 命名空间未引入，加 `using Quoridor.Application;`（已含）。

- [ ] **Step 3: 提交**

```bash
git add src/Quoridor.UI/Scripts/PreviewLayerView.cs
git commit -m "feat(ui): PreviewLayerView 悬浮预览叠绘(ImmediateMesh+Label3D)"
```

---

### Task 14: HudView（CanvasLayer HUD）

**Files:**
- Create: `src/Quoridor.UI/Scripts/HudView.cs`

- [ ] **Step 1: 写实现**

`src/Quoridor.UI/Scripts/HudView.cs`:
```csharp
using Godot;
using Quoridor.Domain.Core;
using Quoridor.UI.Logic;

namespace Quoridor.UI;

/// <summary>CanvasLayer HUD: TopBar(回合/模式/先手) + NotationPanel + WallBudgetBar + Footer(提示/回到开始页)。</summary>
public partial class HudView : CanvasLayer
{
    private Label _top = new();
    private RichTextLabel _notation = new();
    private Label _budget = new();
    private Label _footer = new();
    private Button _home = new() { Text = "回到开始页" };
    private SeatMap _seatMap;

    public event Action? BackToStartRequested;

    public void Init(SeatMap seatMap)
    {
        _seatMap = seatMap;
        _top.Position = new Vector2(10, 10);
        _top.Size = new Vector2(800, 30);
        _notation.Position = new Vector2(900, 10);
        _notation.Size = new Vector2(360, 600);
        _notation.BbcodeEnabled = true;
        _budget.Position = new Vector2(10, 760);
        _footer.Position = new Vector2(10, 720);
        _footer.Size = new Vector2(800, 30);
        _home.Position = new Vector2(700, 720);
        _home.Visible = false;
        _home.Pressed += () => BackToStartRequested?.Invoke();
        AddChild(_top); AddChild(_notation); AddChild(_budget); AddChild(_footer); AddChild(_home);
    }

    public void RefreshTop(GameState state, GameConfig cfg)
    {
        int active = _seatMap.ToDisplayNumber(state.ActivePlayer);
        _top.Text = $"回合: 玩家{active} | 模式: {cfg.Mode} | 先手: 玩家{_seatMap.ToDisplayNumber(cfg.FirstMove)}";
        int w1 = state.PlayerOf(_seatMap.FromDisplayNumber(1)).WallsLeft;
        int w2 = state.PlayerOf(_seatMap.FromDisplayNumber(2)).WallsLeft;
        _budget.Text = $"墙数 — 玩家1: {w1}  玩家2: {w2}";
    }

    public void AppendNotation(IGameEvent e)
    {
        if (e is PawnMoved pm)
            _notation.AppendText($"玩家{_seatMap.ToDisplayNumber(pm.Who)}: {NotationOf(pm.To)}\n");
        else if (e is WallPlaced wp)
            _notation.AppendText($"玩家{_seatMap.ToDisplayNumber(wp.Who)}: 墙{wp.Wall.Anchor.Col},{wp.Wall.Anchor.Row}{(wp.Wall.Orient == WallOrient.Vertical ? "V" : "H")}\n");
    }

    private static string NotationOf(Cell c) => $"{(char)('a' + c.Col)}{c.Row + 1}";

    public void ShowFooter(string text) => _footer.Text = text;

    public void ShowWinner(PlayerId winner)
    {
        _footer.Text = $"玩家{_seatMap.ToDisplayNumber(winner)} 获胜!";
        _home.Visible = true;
    }

    public void ResetNotation() => _notation.Clear();
}
```

- [ ] **Step 2: 验证构建**

Run: `dotnet build src/Quoridor.UI`
Expected: 成功。

- [ ] **Step 3: 提交**

```bash
git add src/Quoridor.UI/Scripts/HudView.cs
git commit -m "feat(ui): HudView CanvasLayer HUD"
```

---

### Task 15: GameViewRoot + GameView.tscn

**Files:**
- Create: `src/Quoridor.UI/Scripts/GameViewRoot.cs`
- Create: `src/Quoridor.UI/Scenes/GameView.tscn`

- [ ] **Step 1: 写 GameViewRoot**

`src/Quoridor.UI/Scripts/GameViewRoot.cs`:
```csharp
using Godot;
using Quoridor.Application;
using Quoridor.Domain.Core;
using Quoridor.UI.Logic;

namespace Quoridor.UI;

public partial class GameViewRoot : Node3D
{
    private MainController? _ctrl;
    private BoardView? _board;
    private PreviewLayerView? _preview;
    private HudView? _hud;
    private Camera3D? _cam;
    private DirectionalLight3D? _light;
    private WorldEnvironment? _env;

    public override void _Ready()
    {
        _ctrl = GetNode<MainController>("/root/MainController");
        BuildScene();
        var cfg = _ctrl.Config!;
        _ctrl.StartSession(cfg);
        _board!.Init(_ctrl);
        _preview!.Init(_board.Layout);
        _hud!.Init(SeatMap.ForFirstMove(cfg.FirstMove));

        _board.CellClicked += OnCellClicked;
        _board.SlotHovered += OnSlotHovered;
        _board.SlotClicked += OnSlotClicked;
        _board.SlotCleared += () => _preview.Clear();
        _hud.BackToStartRequested += OnBackToStart;
        _ctrl.Session!.EventOccurred += OnEvent;

        _hud.RefreshTop(_ctrl.Session.State, cfg);
        _board.Render(_ctrl.Session.State);
        _ctrl.Session.Start();   // 若起手为 AI 同步驱动; 子节点已就绪
    }

    public override void _ExitTree()
    {
        if (_ctrl?.Session is { } s) s.EventOccurred -= OnEvent;
        _ctrl?.EndSession();
    }

    private void BuildScene()
    {
        _light = new DirectionalLight3D { ShadowEnabled = true, Rotation = new Vector3(Mathf.DegToRad(-55), Mathf.DegToRad(30), 0) };
        AddChild(_light);
        _env = new WorldEnvironment();
        var e = new Environment();
        e.GlowEnabled = true;
        e.GlowStrength = 0.8f;
        e.TonemapTonemapper = Environment.TonemapTonemapper.Filmic;
        _env.Environment = e;
        AddChild(_env);
        var cfgBoard = _ctrl!.BoardConfig;
        float center = (cfgBoard.MaxIndex) * 1.0f / 2f;
        _cam = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Size = cfgBoard.Size + 2 };
        _cam.Position = new Vector3(center, cfgBoard.Size + 2, center + 1);
        _cam.Rotation = new Vector3(Mathf.DegToRad(-55), 0, 0);
        AddChild(_cam);

        _board = new BoardView();
        AddChild(_board);
        _preview = new PreviewLayerView();
        AddChild(_preview);
        _hud = new HudView();
        AddChild(_hud);
    }

    private void OnCellClicked(Cell cell)
    {
        GD.Print($"UI click cell {cell}");
        _ctrl!.Session!.Submit(new MovePawnCommand(cell));
    }

    private void OnSlotHovered(SlotId slot)
    {
        var layout = _board!.Layout;
        if (layout.SlotToWall(slot) is not { } wall) return;
        var preview = PreviewService.PoseWall(_ctrl!.Session!.State, wall);
        _preview!.Show(preview, wall);
    }

    private void OnSlotClicked(SlotId slot)
    {
        if (_board!.Layout.SlotToWall(slot) is not { } wall) return;
        GD.Print($"UI click slot {slot} → wall {wall}");
        _ctrl!.Session!.Submit(new PlaceWallCommand(wall));
    }

    private void OnEvent(IGameEvent e)
    {
        // 纪律: 只改视觉, 绝不调 Submit/Start
        switch (e)
        {
            case PawnMoved:
            case WallPlaced:
            case TurnPassed:
                _board!.Render(_ctrl!.Session!.State);
                _hud!.RefreshTop(_ctrl.Session.State, _ctrl.Config!);
                _hud.AppendNotation(e);
                _preview!.Clear();
                break;
            case WallRejected wr:
                _hud!.ShowFooter($"设墙被拒: {RejectReasonText.Of(wr.Reason)}");
                break;
            case MoveRejected mr:
                _hud!.ShowFooter($"走子被拒: {RejectReasonText.Of(mr.Reason)}");
                break;
            case PlayerWon pw:
                _board!.Render(_ctrl!.Session!.State);
                _hud!.ShowWinner(pw.Who);
                break;
        }
    }

    private void OnBackToStart()
    {
        GetTree().ChangeSceneToFile("res://Scenes/StartFrame.tscn");
    }
}
```

- [ ] **Step 2: 写 GameView.tscn**

`src/Quoridor.UI/Scenes/GameView.tscn`:
```
[gd_scene load_steps=2 format=3]

[ext_resource path="res://Scripts/GameViewRoot.cs" type="Script" id="1"]

[node name="GameView" type="Node3D"]
script = ExtResource("1")
```

- [ ] **Step 3: 验证构建**

Run: `dotnet build src/Quoridor.UI`
Expected: 成功。

- [ ] **Step 4: 提交**

```bash
git add src/Quoridor.UI/Scripts/GameViewRoot.cs src/Quoridor.UI/Scenes/GameView.tscn
git commit -m "feat(ui): GameViewRoot 装配 + OnEvent 派发 + GameView.tscn"
```

---

### Task 16: StartFrameView + StartFrame.tscn

**Files:**
- Create: `src/Quoridor.UI/Scripts/StartFrameView.cs`
- Modify: `src/Quoridor.UI/Scenes/StartFrame.tscn`（替换占位）

- [ ] **Step 1: 写 StartFrameView**

`src/Quoridor.UI/Scripts/StartFrameView.cs`:
```csharp
using Godot;
using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Quoridor.UI.Logic;

namespace Quoridor.UI;

public partial class StartFrameView : Control
{
    private OptionButton _variant = new();
    private OptionButton _mode = new();
    private OptionButton _diff = new();
    private OptionButton _first = new();
    private Button _start = new() { Text = "开始对局" };

    public override void _Ready()
    {
        _variant.AddItem("标准 9x9", 0);
        _variant.AddItem("Kid 7x7", 1);
        _mode.AddItem("人机", 0);
        _mode.AddItem("双人 hot-seat", 1);
        _diff.AddItem("简单", (int)Difficulty.Easy);
        _diff.AddItem("中等", (int)Difficulty.Medium);
        _diff.AddItem("困难", (int)Difficulty.Hard);
        _first.AddItem("玩家1 先手", 0);
        _first.AddItem("玩家2 先手", 1);

        int y = 20;
        foreach (var c in new Control[] { _variant, _mode, _diff, _first, _start })
        { c.Position = new Vector2(40, y); c.Size = new Vector2(220, 30); AddChild(c); y += 50; }

        _mode.ItemSelected += idx => _diff.Visible = idx == 0;
        _start.Pressed += OnStart;
    }

    private void OnStart()
    {
        var ctrl = GetNode<MainController>("/root/MainController");
        var variant = _variant.Selected == 1 ? BoardVariant.Kid : BoardVariant.Standard;
        var mode = _mode.Selected == 1 ? MatchMode.HotSeat : MatchMode.VsAi;
        var diff = (Difficulty)(_diff.Selected == -1 ? 0 : _diff.Selected);
        var first = _first.Selected == 1 ? PlayerId.P2 : PlayerId.P1;
        ctrl.Config = new GameConfig(variant, mode, diff, first);
        GD.Print($"StartFrame: variant={variant} mode={mode} diff={diff} first={first}");
        GetTree().ChangeSceneToFile("res://Scenes/GameView.tscn");
    }
}
```

- [ ] **Step 2: 写 StartFrame.tscn**

`src/Quoridor.UI/Scenes/StartFrame.tscn`:
```
[gd_scene load_steps=2 format=3]

[ext_resource path="res://Scripts/StartFrameView.cs" type="Script" id="1"]

[node name="StartFrame" type="Control"]
script = ExtResource("1")
```

- [ ] **Step 3: 验证构建**

Run: `dotnet build src/Quoridor.UI`
Expected: 成功。

- [ ] **Step 4: 提交**

```bash
git add src/Quoridor.UI/Scripts/StartFrameView.cs src/Quoridor.UI/Scenes/StartFrame.tscn
git commit -m "feat(ui): StartFrame 配置项 + 开始对局"
```

---

### Task 17: ClassicTheme + 全量构建

**Files:**
- Create: `src/Quoridor.UI/Themes/ClassicTheme.tres`

- [ ] **Step 1: 建 Theme 资源（最小）**

`src/Quoridor.UI/Themes/ClassicTheme.tres`:
```
[gd_resource type="Theme" format=3]
```
（MVP 用默认主题；Kid 主题 Plan 6 扩展。可后续在编辑器内细化字号/颜色。）

- [ ] **Step 2: 全量构建**

Run: `godot-mono --headless --quit --path src/Quoridor.UI`
Expected: 无报错（导入资源生成 .godot/）。

Run: `dotnet build src/Quoridor.UI`
Expected: 成功。

Run: `dotnet test Quoridor.slnx`
Expected: 全绿（含 Phase A）。

- [ ] **Step 3: 提交**

```bash
git add src/Quoridor.UI/Themes/ClassicTheme.tres
git commit -m "feat(ui): ClassicTheme 占位 + 全量构建通过"
```

---

## Phase D — 集成与验收

### Task 18: 手动验收清单 + 端到端跑通

**Files:**
- Create: `docs/superpowers/plans/2026-07-02-quoridor-ui-acceptance.md`

- [ ] **Step 1: 写验收清单文档**

`docs/superpowers/plans/2026-07-02-quoridor-ui-acceptance.md`（内容为 spec §9.2 的 7 项清单 + 截图位 + 已知坑记录）:
```markdown
# Quoridor UI MVP 手动验收清单

启动: `godot-mono --path src/Quoridor.UI`

## 验收项
- [ ] 1. 人机标准9x9, 玩家1先手, 走完一局至胜(终局显示胜者 + 回到开始页)
- [ ] 2. 人机 Kid7x7, 玩家2先手(AI 先走, 验证座位换位)
- [ ] 3. hot-seat 两人交替走子与设墙
- [ ] 4. 设墙悬浮预览: 合法(绿)+路线+步数; 非法(红, 如切断玩家路径)
- [ ] 5. 墙数耗尽后墙槽不可拾取(input_ray_pickable=false)
- [ ] 6. 终局 → 回到开始页 → 可再开一局(循环)
- [ ] 7. mouse_exit 槽后预览清除

## 已知坑/回归记录
(验收时填写)
```

- [ ] **Step 2: 启动 Godot 跑通清单第 1 项**

Run: `godot-mono --path src/Quoridor.UI`
手动操作 StartFrame → 人机/标准/中等/玩家1先 → 开始 → 在 GameView 点击格子走子、悬浮墙槽预览、点击设墙、直到 AI 或人获胜。
记录任何崩溃/异常到验收文档"已知坑"。

- [ ] **Step 3: 修复发现的问题（如需）**

对清单 1-3 中发现的阻断性 bug 立即修复并补测（若属 UI.Logic 范畴补 xunit；若属 Godot 脚本补 dotnet build + 重跑）。

- [ ] **Step 4: 提交验收结果**

```bash
git add docs/superpowers/plans/2026-07-02-quoridor-ui-acceptance.md
git commit -m "docs(ui): MVP 手动验收清单与结果"
```

- [ ] **Step 5: FF 合并回 master（worktree 收尾）**

按 `superpowers:using-git-worktrees` 收尾：合并 worktree 分支回 master（fast-forward），清理 worktree。

---

## Self-Review

**Spec coverage（逐节对照）:**
- §1.1 MVP 交付 → Task 9-16 全覆盖。✅
- §1.2 推迟项 → 计划未实现 Setting/Replay/Kid 主题/4 人/动画，符合。✅
- §1.3 零改动 → 计划无改 Domain/Application。✅
- §2 架构 → Task 9 (Godot 项目) + Task 11 (Autoload)。✅
- §3 组件 → Task 11/12/13/14/15/16。✅
- §4 数据流 → GameViewRoot (Task 15) 实现开局/走子/设墙/事件刷新。✅
- §5 槽↔WallPos → Task 3/4 (TDD 覆盖)。✅
- §6 先手换位 → Task 5 (SeatsBuilder+SeatMap TDD)。✅
- §7 渲染光影 → Task 15 BuildScene (light/env/camera)。✅
- §8 错误/结转 → Task 10 (logger) + Task 12 (墙数=0 禁用) + Task 15 (OnEvent 不调 Submit)。✅
- §9 测试 → Task 3-7 (UI.Logic TDD) + Task 18 (手动验收)。✅
- §10 项目结构 → Task 1/9 + slnx。✅

**Placeholder scan:** 无 TBD/TODO；每个代码步骤含完整代码。✅
（Task 13 首行 `using` 连写需在实现时纠正为单独行——已在 Step 2 标注。）

**Type consistency:** `SlotToWall`/`WallToSlot`/`CellClicked`/`SlotHovered`/`SlotClicked`/`SlotCleared`/`Show`/`Clear`/`Render`/`Init`/`RefreshTop`/`AppendNotation`/`ShowWinner`/`BackToStartRequested` 跨任务命名一致。✅

无未覆盖项。计划可执行。
