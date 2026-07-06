# Quoridor 项目导览(现状 + .NET/Godot 学习路径)

> 给新 session / 新读者:先读本文件,再跟代码读。配套:`CLAUDE.md`(精炼)、`docs/superpowers/specs/`(设计)、`docs/superpowers/plans/`(计划)。

## 1. 项目现状(2026-07-06)

桌面客户端版 Quoridor(墙棋)。**Plan 1-4 + 体验改进 + 回放功能已 FF 合入 `master`**(HEAD `196ce81`,已推送 origin)。

| Plan | 内容 | 测试 | 状态 |
|---|---|---|---|
| 1 Domain Core | 纯 C# 不可变 GameState + 命令/事件 + 规则 + BFS + 代数记谱 | 74 | ✅ |
| 2 AI | IQuoridorAi + GreedyAi + MinimaxAi(Alpha-Beta) | (含在 Domain) | ✅ |
| 3 Application | GameSession/PreviewService/ReplayController/IAppLogger/AiPlayerFactory | 41 | ✅ |
| 4 Godot UI MVP | StartFrame + GameView 2.5D 棋盘 + 人机/hot-seat + 设墙预览 | 24 | ✅ 验收项1通过 |
| 体验改进(2026-07-06) | AI 异步化(后台决策) + Minimax 根级并行 + 状态提示 + 投降 | +10 | ✅ |
| 回放功能(2026-07-06) | 预置 18 局棋局库 `ReplayLibrary` + 回放 UI(⏮⬅➡⏭,复用 ReplayController) | +21 | ✅ |

**测试**:`dotnet test Quoridor.slnx` → Domain 74 + Application 41 + UI.Logic 45 = 160 全绿。
**运行**:`cd src/Quoridor.UI && godot-mono --path .`。
**已推 origin**。

## 2. 架构:三层单向依赖

```
Quoridor.UI (Godot)  →  Quoridor.UI.Logic (纯 C# 桥)  →  Quoridor.Application  →  Quoridor.Domain
   Node/Control            坐标/座位/文案            GameSession/Preview           GameState/规则
```

- **Domain**(`src/Quoridor.Domain/`):零 Godot 依赖,纯 C#,可单测。不可变 `GameState`(sealed record class),命令/事件 record,`RuleEngine` 校验并应用,`PathFinder` BFS。
- **Application**(`src/Quoridor.Application/`):`GameSession` 编排中枢(Submit→RuleEngine→广播事件→驱动 AI);`PreviewService` 算设墙预览;`ReplayController` 回放;`IAppLogger` 日志抽象;`Seats/IPlayer`(Human/Ai)。
- **UI.Logic**(`src/Quoridor.UI.Logic/`):纯 C#,零 Godot。坐标映射 `BoardLayout`、座位 `SeatsBuilder`/`SeatMap`、文案 `RejectReasonText`、logger 占位符 `NamedPlaceholder`。**可单测**(Godot 脚本不能)。
- **UI**(`src/Quoridor.UI/`):Godot 4.7 mono。薄 Node 脚本,**程序化构建子节点**(`AddChild`),不手写 .tscn 的 uid/instance。`MainController` 为 Autoload 跨场景持久。

## 3. 如何运行/构建/测

```bash
# 全量测试
dotnet test Quoridor.slnx

# 只构建 Godot 项目
dotnet build src/Quoridor.UI/Quoridor.UI.csproj

# 跑游戏(GUI)
cd src/Quoridor.UI && godot-mono --path .

# headless 烟雾(只加载主场景,看有无崩溃)
cd src/Quoridor.UI && godot-mono --headless --verbose --quit-after 120 --path .
```

- shim 是 **`godot-mono`**(不是 `godot`),Scoop 装的 .NET 版。`/c/Applications/Scoop/shims/godot-mono.exe`。
- .NET 10.0.301 SDK。Godot 4.7 stable mono。
- Godot 项目 csproj 须 pin `Sdk="Godot.NET.Sdk/4.7.0"`(不带版本 dotnet 解析不了);**不要**加显式 `<PackageReference GodotSharp>`(SDK 自带)。

## 4. 代码导览:学 .NET + Godot(按层,带文件指针)

### 4.1 Domain — 纯 C# / .NET 基础

- `Core/GameState.cs`:sealed record class,不可变。`With*` 模式返回新状态。**学 .NET**:record、`init` setter、`ImmutableArray`。
- `Core/Commands.cs` / `Core/Events.cs`:命令/事件 record(`MovePawnCommand`、`WallPlaced`、`WallRejected`...)。**学 .NET**:record 单参数构造、模式匹配。
- `Rules/RuleEngine.cs`:`ValidateAndApply` 返回 `ApplyResult`(含 RejectReason)。**学**:switch 表达式、`ImmutableArray` 重建。
- `Path/PathFinder.cs` + `Path/BoardGraph.cs`:BFS 最短路 + 可达性。**学**:队列 BFS、`HashSet`。
- `Notation/NotationService.cs`:Modern Algebraic Notation 编解码。
- `AI/MinimaxAi.cs`:Alpha-Beta 剪枝。**学**:递归、排序。

测试:`tests/Quoridor.Domain.Tests/` — xUnit `[Fact]`/`[Theory]`/`[InlineData]`。

### 4.2 Application — 编排与抽象

- `GameSession.cs`:**核心**。构造 `(BoardConfig, IReadOnlyList<IPlayer>, IAppLogger?)`;`Submit(IGameCommand)`→RuleEngine→广播 `event Action<IGameEvent>? EventOccurred`→`DriveAi`。**学**:`event`、`params object[]`、可空参数默认值。
- `PreviewService.cs`:`PoseWall(GameState, WallPos)→PreviewResult`(含 `Legal`/`Routes`)。读它看"预览"如何复用 Domain 的 PathFinder。
- `Seats/AiPlayerFactory.cs`:Difficulty→AI 映射(Easy=Greedy,Medium/Hard=Minimax)。
- `Logging/IAppLogger.cs` + `LogLevel` + `NullAppLogger`:日志抽象(Domain 不加日志,由 Application 调用)。

测试:`tests/Quoridor.Application.Tests/`。

### 4.3 UI.Logic — 纯 C# 桥(关键坐标数学在这)

- `BoardLayout.cs`:**坐标映射核心**。
  - `CellToWorld(Cell)→(float X,Y,Z)`:格中心 = `((c+0.5)*s, 0, (MaxIndex-r+0.5)*s)`。row 0 在近端(屏幕下方),向上递增;Z 翻转。
  - `WorldToCell(x,z)→Cell?`:用 `MathF.Floor`(格内任意点属该格,中心往返一致)。
  - `WallCenter(WallPos)→(X,Y,Z)`:墙中心在格交点 `((c+1)*s, (MaxIndex-r)*s)`(水平/垂直墙同中心,朝向决定跨向)。
  - `SlotToWall`/`WallToSlot`/`PickableSlots`:槽(SlotId)↔墙(WallPos)映射。
  - **学 .NET**:值元组、`MathF`、`IEnumerable` yield、readonly record struct。
- `SeatsBuilder.cs` + `SeatMap.cs`:先手换位(VsAi 先手=P2 时 P1 座位=AI 玩家2)。
- `NamedPlaceholder.cs`:命名占位符 `{Name}` 按位置填(不能用 `string.Format`,命名 token 会抛)。

测试:`tests/Quoridor.UI.Logic.Tests/BoardLayoutTests.cs` — 含格中心/WallCenter 锁定测试。

### 4.4 UI (Godot) — Godot C# 关键模式

> Godot 4.7 mono。所有继承 Godot 类型 的类须 `partial`(源生成器注入信号/绑定)。

- `project.godot`:引擎配置。`[autoload] MainController="*res://Scripts/MainController.cs"`(autoload 跨场景持久,`*`=singleton);`[dotnet] project/assembly_name="Quoridor.UI"`(**须与 csproj 产物名一致**,否则 `Failed to load project assembly`)。
- `Quoridor.UI.csproj`:`Sdk="Godot.NET.Sdk/4.7.0"`,`net10.0`,`EnableDynamicLoading=true`,ProjectReference Application + UI.Logic。
- `Scripts/MainController.cs`:Autoload Node。持有 `GameConfig?`/`GameSession?`/`GodotAppLogger`;`StartSession`/`EndSession`。**学 Godot**:autoload、`GetNode<T>("/root/MainController")`。
- `Scripts/GodotAppLogger.cs`:`IAppLogger`→`GD.Print/PushWarning/PushError` + 内存 ring buffer。**学**:桥接 Application 抽象到 Godot API、`NamedPlaceholder.Format`。
- `Scripts/BoardView.cs`(`Node3D`):**最值得读**。程序化构建平板+网格槽+格子点击区+棋子+墙;幂等 `Render(state)`;`Area3D.InputEvent` 信号接点击。**学 Godot**:`MeshInstance3D`+`BoxMesh`/`PlaneMesh`/`CylinderMesh`、`StandardMaterial3D`(AlbedoColor/Roughness/NoDepthTest)、`Area3D`+`CollisionShape3D`+`BoxShape3`+`InputRayPickable`、`InputEventEventHandler`(签名 `(Node,InputEvent,Vector3,Vector3,long)`)、`CallDeferred`/`QueueFree`、`event Action<T>`。
- `Scripts/PreviewLayerView.cs`(`Node3D`):`ImmediateMesh` 画路线 + `Label3D` 步数 + 候选墙半透明(`TransparencyEnum.Alpha`)。**学**:`ImmediateMesh.SurfaceBegin/AddVertex/End`、`SurfaceSetMaterial`、值元组不可 `with`(改解构)。
- `Scripts/HudView.cs`(`CanvasLayer`):TopBar/Notation(RichTextLabel)/WallBudget/Footer/Button。**学**:Control 布局、`MouseFilter.Ignore`(让点击穿透到 3D 棋盘)、`Button.Pressed +=`。
- `Scripts/GameViewRoot.cs`(`Node3D`):**装配枢纽**。`_Ready` 里 GetNode autoload → BuildScene(光/环境/相机/三子视图)→ StartSession → 订阅事件 → `Session.Start()`;`OnEvent(IGameEvent)` 派发(只改视觉,**不重入 Submit/Start**);`_ExitTree` 解订阅+EndSession。**学 Godot**:`DirectionalLight3D`、`WorldEnvironment`+`Environment`(Glow/TonemapMode/ToneMapper)、`Camera3D`(Perspective/Fov/LookAt/Position)、`SceneTree.ChangeSceneToFile`、override `_Ready`/`_ExitTree`。**相机俯角从水平面算**(60°,0°=平视,90°=正俯视)。
- `Scripts/StartFrameView.cs`(`Control`):OptionButton 配置 + 开始按钮 → 写 `MainController.Config` → `ChangeSceneToFile(GameView.tscn)`。**学**:OptionButton.AddItem/Selected/ItemSelected、`GetNode` autoload、场景切换。
- `Scenes/*.tscn`:最小根节点 + 脚本绑定(`[ext_resource path="res://Scripts/X.cs" type="Script"]`)。Godot 4 用 `path=`(+ 运行时分配 uid)。

## 5. 关键踩坑/决策(避免重蹈)

- **TFM 不是问题**:Godot 4.7 GodotSharp NuGet 只发 `lib/net8.0`,但 `GodotPlugins.runtimeconfig.json` 用 `rollForward:LatestMajor`,前滚到本机 .NET 10 → **net10.0 可加载,别降 net8.0**。曾误判 TFM,实为 `assembly_name` 拼写不一致。
- **CLI 不触发 Godot C# 构建**:`--build-solutions`/`--editor --quit` 都不触发 MSBuild;只能 GUI Build 按钮 / F5。但 `dotnet build` + assembly_name 一致时 runtime 直接加载。
- **Godot 4.7 API 名与文档/计划可能有出入**:Task 执行中用反射核验 GodotSharp.dll 实际成员,发现 `TransparencyModeEnum`→实为 `TransparencyEnum`、`TonemapTonemapper`→`TonemapMode`/`ToneMapper`、`CylinderMesh.Radius`→`TopRadius`/`BottomRadius`、`InputEventEventHandler` 签名是 `(Node,...,long)` 非 `(Camera3D,...,int)`。**遇枚举/属性编译错,反射查 GodotSharp.dll**。
- **值元组不支持 `with`**:`CellToWorld` 返回 `(float,float,float)`,不能 `with { Y=... }`;先解构再 `new Vector3`。
- **ImmediateMesh + MaterialOverride 可能不生效**;用 `SurfaceSetMaterial` 保险。
- **GameState 是引用类型 record**:`GameState?` 无 `.Value`,用 `r.State!`。
- **墙几何**:水平墙 anchor(c,r) 阻竖直通道 (c,r)-(c,r+1) 与 (c+1,r)-(c+1,r+1);垂直墙阻水平通道 (c,r)-(c+1,r) 与 (c,r+1)-(c+1,r+1);同 anchor 不同朝向="+"字交叉**非法**(T 字合法)。
- **EventOccurred 回调禁重入 Submit/Start**(GameSession 无重入保护)。
- **Domain 不加日志**;日志在 Application `IAppLogger`,Godot 端 `GodotAppLogger` 桥接。

## 6. 后续(未立项,按需开 worktree)

- 验收项 2-7:Kid 7×7 / 玩家2先手(座位换位)/ hot-seat / 预览显示确认 / 墙耗尽禁拾 / 循环再开 / mouse_exit 清除。
- **Plan 5**:Setting 面板(Replay 回放视图已于 2026-07-06 完成:18 局预置 + ⏮⬅➡⏭ 控制条,复用 ReplayController)。
- **Plan 6**:Kid 主题资产 + 4 人模式 + 动画(spec 已规划)。
- **UI 美化计划**:用 `Container`/`Anchor` 重做布局(替代硬编码坐标)+ 真实 `Theme`(字号/配色/间距)+ 棋子/墙材质光影 + 悬停高亮 + 走子动画。这是"UI 丑"的正解。

## 7. 执行方法论(给新 session)

- **subagent-driven-development** + **git worktree** 隔离:每 Plan 一棵 worktree,完成后 FF 合并回 master 并清理。superpowers skill 体系。
- **TDD**:Domain/Application/UI.Logic 纯逻辑层全 TDD;Godot 脚本(Phase C)无法单测,验证靠 `dotnet build` + 手动验收。
- **计划文件**:每 Plan 一个 `docs/superpowers/plans/<date>-<name>.md`,18 任务级 checkbox + 完整代码 + Self-Review。执行前先写 spec + plan,经对抗式自审。
