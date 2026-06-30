# Quoridor 游戏项目设计文档

- 日期：2026-06-30
- 状态：已通过设计评审，待编写实现计划
- 作者：melod_yi + Claude（brainstorming）

## 1. 背景与目标

这是一个 10 年前学生时代未完成的项目，现清空目录重新实现。Quoridor（墙棋 / 步步为营）是一款 2–4 人的抽象策略棋盘游戏。

**本期目标（首期交付）**

- 桌面 GUI，完整对局流程。
- 2 人 / 4 人游戏可选，座位可任意配置为人类或 AI（含混搭）。
- 标准模式（9×9 棋盘、20 面墙）与 Quoridor Kid（7×7 棋盘、16 面墙、老鼠与奶酪主题）。
- 本地单人对 AI、本地多人（hot-seat，同设备轮流）。
- Modern Algebraic Notation 记录与表示棋局，支持导出 / 导入 / 回放。
- UI "辅助模式"（可开关）：设墙时鼠标悬浮预览各棋子最优路线与步数，点击确认后落墙。

**非目标（现期不实现，但技术选型预留）**

- 联机对局、移动端 app 的实际实现与规划。
- 但技术选型必须使上述方向在未来可叠加。

**额外目标**

- 通过本项目学习并应用现代游戏引擎（Godot 4）与 C#。

## 2. 游戏规则要点（已核实）

来源：Wikipedia、Gigamic 官方规则、Geeky Hobbies、BoardGameGeek、Major Fun 等。

### 2.1 标准模式

- 9×9 = 81 格棋盘。
- 墙总数 20 面：2 人局每方 10 面；4 人局每方 5 面。
- 棋子起始位置（Modern Algebraic Notation 坐标，列 a–i 左→右，行 1–9 下→上，左下角 a1）：
  - 2 人：P1 起 e1（目标第 9 行）；P2 起 e9（目标第 1 行）。
  - 4 人：P1 起 e1（目标第 9 行）；P2 起 a5（目标 i 列）；P3 起 e9（目标第 1 行）；P4 起 i5（目标 a 列）。
- 每回合二选一：移动棋子 或 放一面墙；墙用尽后只能移动棋子。
- 率先把棋子移动到自己的目标边（该边任意一格）即获胜。

### 2.2 棋子走法

- 普通一步：前 / 后 / 左 / 右移动一格，不能斜走，不能穿墙。
- 直跳：与对方棋子相邻且二者之间无墙时，可跳到对方身后格。
  - 若身后格被墙挡住、越出棋盘、或被第三枚棋子占据，则不能直跳。
  - 不能连跳多枚棋子。
- 斜跳：直跳不可行时，可移动到对方棋子左侧或右侧相邻格之一。
  - 该侧向移动同样不能穿墙，侧方格被墙隔开则该侧不可落子。
  - 4 人局：每回合只能跳过一枚棋子（由"只走一步"自然保证）。
- 跳子 / 斜跳在记谱中无专门符号，一律以最终落点坐标记录。

### 2.3 设墙规则

- 每面墙覆盖 2 个格子的边，放在相邻两格之间的凹槽。
- 水平 / 垂直两种朝向，不可重叠，一旦放置不可移动或移除。
- **可达性规则**：不得放置会完全切断任一玩家到达其目标边全部路径的墙——必须始终为每个棋子留出至少一条到目标边的通路。

### 2.4 Quoridor Kid

- 7×7 棋盘。
- 墙总数 16 面（标准版为 20 面）。
- 用老鼠（mouse）代替棋子，目标边放奶酪（cheese）作终点标记，颜色对应。
- 玩法规则与标准版一致。BoardGameGeek 标注 Quoridor Junior 支持 2–4 人。
- **每方墙数**：2 人局每方 8 面、4 人局每方 4 面（已确认，按标准版比例）。

### 2.5 Modern Algebraic Notation

- 坐标：列 a–i（9×9）/ a–g（7×7），左→右；行 1–9 / 1–7，下→上。左下角 a1。
- 棋子移动：记落点格坐标，如 `e2`。
- 墙放置：记"基准格 + 朝向字母"。基准格取该墙所触及四个格子中最靠近 a1 的格；朝向用小写 `h`（水平）或 `v`（垂直），如 `e3v`。
- 跳子 / 斜跳：无专门符号，记落点格。
- 4 人局区分玩家：靠回合内列位，每个回合号下按 P1→P2→P3→P4 顺序排列走子。
- 完整记谱示例：
  - 2 人：`1. e2 e8 2. e3 e7`
  - 4 人：`1. e2 b5 e8 h5 2. e3 c5 e7 g5`
- 注释续谱用 `...` 占位，如 `3. e6h` / `3... e3h`。
- 局面串（FEN-like，可选）：`[横墙]/[竖墙]/[棋子坐标]/[剩余墙数]/[活跃玩家]`。
- 历史上 Quoridor 有多套记谱法（ASCII / NESW / Glendenning / Modern Algebraic）。本项目明确采用 Modern Algebraic Notation，避免与 NESW（大写字母、`/`、`-`、方向相反）混淆。

## 3. 总体架构

三层 + 可插拔 AI，全部 C#，跨平台编译产物。

```
┌─────────────────────────────────────────────────────┐
│  Quoridor.UI  (Godot 4 .NET 项目, 桌面首期)          │
│  StartFrame / GameView / SettingView / ReplayView   │
│  └─ 仅依赖 Application 层接口，不直接碰 Domain 内部   │
├─────────────────────────────────────────────────────┤
│  Quoridor.Application  (纯 C# 类库, 可移植)          │
│  GameSession · Presenter · Command 调度 · 记谱服务   │
│  AI 适配器(AIPlayer 适配 IPlayer 门槛接口)            │
├─────────────────────────────────────────────────────┤
│  Quoridor.Domain  (纯 C# 类库, 零 Godot 依赖)        │
│  · Core:  不可变 GameState + Command/Event          │
│  · Rules: 走子/设墙/跳子合法性、可达性(无向图)        │
│  · Path:  BFS 最短路径(辅助模式 + 评估函数复用)      │
│  · Notation: Modern Algebraic 编/解码               │
│  · AI:    IQuoridorAi 接口 + MinimaxAB/贪婪实现      │
└─────────────────────────────────────────────────────┘
        ↑ 未来联机 = Application 层外挂 NetworkAdapter，
          把 Domain 事件序列化经 WebSocket 收发；
          移动端 = 同一 Domain/Application + 换 UI。
```

**关键约束**

- **Domain 零 Godot 依赖**：GameState、所有规则、路径、记谱、AI 均为纯 C#，可被任意宿主（Godot、未来服务端、单元测试）引用。这是"稳定 domain"的硬性边界。
- **不可变状态 + 命令/事件**：`GameState.Apply(Command) → (newState, Event[])`。旧状态不被修改；回放 / 悔棋 / 联网同步都建立在"事件序列重建状态"上。
- **AI 与人统一门槛**：Domain 定义 `IPlayer`（人 / AI 都实现"在给定状态产出一个 Move"的契约），`GameSession` 不关心座位是人还是 AI——为 4 人混搭和未来联机留位。
- **模块依赖方向**：UI → Application → Domain，单向不回头。Domain 不引用上层。

### 3.1 本地版与联机版共用同一 Domain

同一份 Domain 程序集可跑在多个宿主：

- 本地模式（现期）：Godot 客户端内嵌 Domain（权威），无服务端。
- 联机模式（未来）：服务端内嵌 Domain（权威），客户端只发命令、收事件、渲染；客户端可仍内嵌同一 Domain 做"辅助模式只读预览"与"本地合法性预判"。
- 单元测试：测试宿主内嵌 Domain。

### 3.2 联机模式下的"本地算 + 服务端复检"

用户期望：客户端本地即可计算操作合法性与辅助模式路径，服务端仅做权威复检防作弊。本设计天然满足：

- 辅助模式预览：纯本地，零网络。客户端本地 Domain 临时叠墙 → Reachability + PathFinder 算路线 / 步数 → 本地渲染。
- 操作合法性：客户端本地 `ValidateAndApply` 先算；非法则本地提示、不发服务端；合法才把命令发服务端。
- 服务端权威复检：服务端 Domain 同一份代码再校验；合法则广播事件，非法（作弊 / 抢回合 / 状态失步）则回送 Rejected，客户端以服务端事件为准重建状态。
- 客户端乐观落子可点击即显示；两客户端并发提交时先到者采纳、后到者回滚，由"服务端事件 = 唯一真相"兜底。

## 4. Domain 核心数据模型与命令 / 事件

### 4.1 坐标系

Modern Algebraic Notation 约定：列 a–i（9×9）或 a–g（7×7），左→右；行 1–9 / 1–7，下→上；左下角 a1。内部用 0 基索引 `(col, row)`，记谱层负责与字母坐标互转。

### 4.2 核心类型（全部不可变，record / readonly struct）

```csharp
record Cell(int Col, int Row);                          // 格子坐标
record WallPos(Cell Anchor, WallOrient Orient);         // 墙位置：基准格=最靠 a1 的格
enum  WallOrient { Horizontal, Vertical }
record Pawn(PlayerId Owner, Cell Pos);
record PlayerState(PlayerId Id, int WallsLeft, Cell Start, EdgeGoal Goal);

record GameState(
    BoardConfig Config,            // 9×9/20墙 或 7×7/16墙
    ImmutableArray<PlayerState> Players,
    ImmutableArray<Pawn> Pawns,
    ImmutableArray<WallPos> Walls,
    PlayerId ActivePlayer,
    Phase Phase,                   // Running / Finished
    Optional<PlayerId> Winner
);

// 命令（意图，可被拒绝）
interface IGameCommand {}
record MovePawnCommand(Cell To);
record PlaceWallCommand(WallPos Wall);
// 跳子/斜跳统一归为 MovePawnCommand —— 目标格不同而已，规则层判定

// 事件（已发生的事实，可序列化）
interface IGameEvent {}
record PawnMoved(PlayerId Who, Cell From, Cell To, MoveKind Kind); // Step/Jump/DiagonalJump
record WallPlaced(PlayerId Who, WallPos Wall);
record WallRejected(PlayerId Who, WallPos Wall, RejectReason);     // UI 反馈用
record MoveRejected(PlayerId Who, Cell To, RejectReason);
record PlayerWon(PlayerId Who);
record TurnPassed(PlayerId Next);
```

### 4.3 命令处理流程（Domain 唯一状态变更入口）

```csharp
(Option<GameState> newState, Option<IGameEvent[]> events) =
    RuleEngine.ValidateAndApply(state, command)
```

- 校验顺序：是否轮到该玩家 → 命令合法性（走子可达性 / 设墙不重叠）→ 设墙可达性规则（无向图判连通，不能切断任一玩家到目标边的全部路径）→ 墙数是否够。
- 合法：生成新 `GameState`（不可变重建）+ 事件序列。
- 非法：返回 `WallRejected` / `MoveRejected`（事件，供 UI 提示），状态不变。
- 走子规则：普通一步四方向；相邻对方棋子且中间无墙 → 直跳身后格；身后被墙 / 边界 / 第三子挡住 → 斜跳到对方左右侧格（仍受墙约束）。跳子合法性由规则层统一判定，调用方只给目标格。

### 4.4 记谱与事件的对应

一个事件 = 记谱中的一手。`NotationService` 把 `IGameEvent` 序列化为 `e2` / `e3v` 等 token；反向把记谱串解析回命令序列回放整局。墙的基准格约定（最靠 a1）在 `NotationService` 内统一处理。

## 5. 规则引擎与路径 / 合法性算法

棋盘小（≤81 格），对复杂度不敏感，一律用最直接、可读、易测的算法，不做性能投机。

### 5.1 棋盘图模型

- 格子 = 节点。两相邻格之间若无墙阻挡，则有一条边。
- 墙作用在"格之间的边"上：一面墙同时抹掉两条相邻边（墙覆盖 2 个格的边）。设墙合法性 = 抹边后查询连通性。
- 维护轻量无向图（不可变、值类型友好；旧项目 UDGraph 思路保留并重写）。

### 5.2 两类核心查询

- `PathFinder.ShortestPath(state, pawn, goalEdge) → (distance, path)`：BFS（无权图最短路）。辅助模式"各棋子最优路线与步数"与 AI 评估函数均复用。
- `Reachability.AllPlayersCanReachGoal(state) → bool`：设墙时对每个玩家各跑一次 BFS，确认仍存在到目标边的路径；不通过则该墙非法。复杂度 ≤4 玩家 × BFS(≤81 节点)，开销可忽略。

### 5.3 走子合法性

判定 `from→to` 是否合法时，构造"候选移动集"：枚举四方向，遇对方棋子则扩展直跳 / 斜跳候选，再检查 `to` 是否在候选集且中间无墙。避免把跳子规则散进多个分支。

跳子规则判定顺序：
- 相邻格为空 → 可移入。
- 相邻格有对方棋子、身后格在棋盘内且无墙 → 可直跳到身后格。
- 身后格被墙挡 / 越界 / 被第三子占 → 可斜跳到对方棋子左右两侧格（每侧仍单独判墙）。
- 4 人局每回合最多跳过一枚棋子（由"只走一步"自然保证）。

### 5.4 AI 模块

```csharp
interface IQuoridorAi {
    Move Choose(GameState state, Difficulty difficulty);
}
```

- `GreedyAi`（低难度）：最短路贪心 + 偶尔给对手设墙。
- `MinimaxAi`（中 / 高难度）：Alpha-Beta 剪枝，深度按难度档（如 2 / 3 / 4），评估函数 = `对手最短步数 − 自己最短步数 + 墙数权重`，复用 `PathFinder`。动作排序：先试"缩短自己路径 / 拉长对手路径"的墙与前进步，提升剪枝效率。
- 候选动作生成复用规则引擎的合法走子集 + 合法设墙集，保证 AI 永不下出非法手。

## 6. 数据流

### 6.1 正常一回合

```
玩家输入(点击格子/墙位)
  → UI 构造 IGameCommand
  → Application.GameSession.Submit(cmd)
  → Domain.RuleEngine.ValidateAndApply(state, cmd)
       合法 → (newState, events)
       非法 → (state 不变, Rejected 事件)
  → GameSession 用 newState 替换当前状态
  → 向所有订阅者广播 events
  → UI 收到事件刷新棋盘 + 记谱面板追加一手
  → 若轮到 AI：GameSession 调用 IQuoridorAi.Choose(newState) → 自动构造 cmd 回到起点
```

人手与 AI 手走完全相同的 `Submit → ValidateAndApply → 广播` 通道，`GameSession` 不区分座位类型。

### 6.2 辅助模式（设墙悬浮预览）

```
鼠标悬浮墙位(hover)
  → UI 调 Application.PreviewService.PoseWall(state, candidateWall)
  → Domain: 临时把 candidateWall 叠进 state
       · Reachability 校验合法性(是否切断任一玩家)
       · 对每个 Pawn 跑 PathFinder.ShortestPath → 路线 + 步数
  → 返回 PreviewResult { Legal, Routes[], StepCounts[] }
  → UI 在棋盘上叠绘各棋子最优路线 + 步数标签；非法则墙位标红
点击确认 → 走正常 Submit 通道落墙
鼠标移开 → 清除预览
```

预览是只读的——`PoseWall` 不改真实状态，只在内存里临时叠加一面墙做计算。

### 6.3 记谱导出 / 导入与回放

```
导出: 事件序列 → NotationService.Encode → "1. e2 e8 2. e3 e7 ..." 字符串(可存文件)
导入: 字符串 → NotationService.Decode → 命令序列
回放: 从初始 state 起，逐条 Apply 命令重建状态；UI 提供 ⏮ ⬅ ⬜ ➡ ⏭ 步进与跳转
```

回放复用同一 `ValidateAndApply`，回放中的状态与正常对局完全同构，辅助模式路线预览在回放界面也可用。

## 7. 错误处理

- Domain 永不抛业务异常：非法操作以 `Rejected` 事件返回，状态不变。UI 据此显示提示（"该位置会切断路径"等）。
- 记谱解析错误：返回结构化 `ParseError(offset, reason)`，UI 高亮出错的那一手，而非整段失败。
- AI 超时 / 异常：`IQuoridorAi` 实现内部 try-catch，异常时回退到 `GreedyAi` 一手，保证对局不卡死并记日志。
- 日志：Application 与 Domain 关键方法入口 / 出口、规则拒绝、AI 决策（深度 / 评估值 / 选中动作）均落日志。

## 8. 测试策略

遵循"每个问题都应加测试用例"。Domain 与 Application 层全面单测，UI 层轻量手测。

| 层 | 框架 | 覆盖重点 |
|----|------|----------|
| Domain | xUnit（纯 C# 类库，脱离 Godot 可跑） | 规则、路径、可达性、记谱、AI |
| Application | xUnit | GameSession 命令调度、事件广播、回放 |
| UI (Godot) | 手测 + 少量集成 | 渲染、辅助模式交互、输入响应 |

### 8.1 Domain 单测重点用例

- 走子：四方向普通步；越界拒；穿墙拒。
- 跳子边界：直跳（身后空）、直跳被墙挡→转斜跳、直跳身后越界→转斜跳、斜跳侧方也被墙挡→非法、4 人第三子阻挡场景。
- 设墙：墙不重叠；墙越界 / 跨格非法；切断某玩家全部路径→非法（核心可达性用例）；墙数用尽后只能走子。
- 胜负：棋子到达目标边→`PlayerWon`；4 人各自目标边正确判定。
- 路径：BFS 最短步数与已知局面一致；无路径时返回 ∞ / 不可达。
- 记谱往返：`Encode(events) → Decode → 重建状态` 与原状态逐字段相等；墙基准格（最靠 a1）编码正确；2 人 / 4 人回合排列正确；非法记谱串返回结构化 `ParseError`。
- AI：`GreedyAi` / `MinimaxAi` 永不下出非法手（对一批随机合法局面验证）；`MinimaxAi` 在已知杀棋局面能走出制胜手。

### 8.2 Application 单测

用假 `IPlayer`（人 / AI 都实现它）驱动 `GameSession`，断言事件序列与状态转换；回放导入导出闭环测试。

### 8.3 回归用例化

每个修复的 bug 都补一条单测固化。规则边界用例集中放在 `Domain.Tests/Cases/`，方便复用为 AI 自对弈的随机局面生成器。

### 8.4 测试与 CI

Domain.Tests 是普通 .NET 测试项目，`dotnet test` 即可跑，不依赖 Godot 引擎、不需要图形环境——规则逻辑可在无显示的 CI / 服务端环境里持续验证。

## 9. 待确认项

- UI 视觉风格、棋盘美术、Kid 模式老鼠 / 奶酪资产：留待后续 UI 设计阶段（可用视觉伴侣辅助）。
- 首期仅桌面 Windows 导出；macOS / Linux 导出可在 Godot 侧低成本开启，按需。
- 联机与移动端：现期不实现不规划，仅技术选型预留。
