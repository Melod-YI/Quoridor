# Quoridor UI 设计文档（Plan 4：Godot 桌面客户端 MVP）

> 关联：上层设计见 `2026-06-30-quoridor-design.md`（§3 架构 / §6 数据流 / §9 待确认项）。
> 本文档覆盖 Plan 4 MVP 范围。Setting/Replay/Kid 主题资产拆至 Plan 5/6。
> 本 spec 已经过对抗式自审 + 代码核实（GameSession/PreviewService/BoardGraph/GameSetup/WallPos）。

## 1. 目标与范围

### 1.1 MVP 交付

- **StartFrame（简）**：收集开局配置 → 进入 GameView。
- **GameView**：3D 棋盘 + 倾斜相机 + 实时光影；走子 / 设墙交互；辅助模式悬浮预览（路线 + 步数叠绘）；HUD（记谱面板、墙数、当前回合、终局提示、回到开始页）；人机与 hot-seat 双模式。
- **持久控制层**：`MainController`（Autoload）跨场景持有 `GameSession` 与配置。
- **纯逻辑库**：`Quoridor.UI.Logic`（零 Godot 依赖，可单测）承载坐标映射 / 座位构造 / 文案。

### 1.2 明确推迟（不在 Plan 4）

- SettingView（独立设置面板，含 AI 难度/主题/音效等持久化）→ Plan 5。MVP 的 AI 难度在 StartFrame 选。
- ReplayView（记谱导入 / 步进回放）→ Plan 6。MVP 终局后仅显示胜者，不提供回放。
- Kid 主题资产（老鼠 / 奶酪贴图、低模、Kid 专属主题资源）→ Plan 6。MVP 用纯几何 + 标准材质；Kid **模式（7×7 规则）** 在 MVP 内，Kid **主题（资产）** 推迟。
- 4 人对局（4 座位轮转 / 4 目标边）→ 未来 Plan。MVP 仅 2 人。
- 联机 / 移动端 → 不规划。
- 棋子移动动画 / AI 思考延迟 → Plan 5。MVP 棋子瞬移无过渡，AI 手同步广播。

### 1.3 依赖现状（零改动）

- Domain 已支持 `BoardConfig.Kid`（7×7）、`BoardVariant`、`WallBudget`、`GameSetup.CreateKid2P`。
- Application 已支持 `Difficulty`（Easy=Greedy / Medium、Hard=Minimax）与 `AiPlayerFactory.Create(id, difficulty)`。
- 结论：**Plan 4 不改 Domain / Application**，仅消费其公开 API。结转项中影响 UI 的问题在本计划内以 UI 侧纪律兜底（见 §8）。

## 2. 架构

### 2.1 分层（沿用 §3，单向依赖）

```
Quoridor.UI            (Godot 4.7 mono, net10.0)  ── 引用 ──> Application ──> Domain
  ├ MainController (Autoload, 跨场景持久)
  ├ Scenes/*.tscn  (StartFrame / GameView)
  ├ Scripts/*.cs   (薄 Node 脚本, 仅渲染与输入)
  └ 引用 Quoridor.UI.Logic (纯 C# UI 逻辑, 零 Godot 依赖, 可单测)
```

`Quoridor.UI.Logic` 为新增的**纯 C# 类库**（net10.0，无 GodotSharp 依赖），承载所有与 Godot 无关的 UI 侧逻辑：格 ↔ 世界坐标、槽 ↔ `WallPos`、命中判定、座位构造、拒绝文案。它引用 Domain（`Cell`/`WallPos`/`BoardConfig`）与 Application（`IPlayer`/`AiPlayerFactory`/`Difficulty`），均为纯 C#，传递依赖无环。Godot 节点调用它做换算，测试项目（net10.0）直接引用它做单测——与现有 Domain/Application 的分层哲学一致。

### 2.2 屏幕流转（方案 A：每视图独立场景 + Autoload）

- `MainController` 注册为 Godot **Autoload**（项目级单例 `Node`，跨场景存活），持有：
  - `GameConfig?`（StartFrame 产出的开局配置 POCO，见 §3.1）
  - `GameSession?`（仅 GameView 在场期间非空）
  - UI 状态（辅助模式开关等；MVP 默认开）
- 视图切换：`GetTree().ChangeSceneToFile("res://Scenes/GameView.tscn")`。
- **GameSession 生命周期**：StartFrame **不构造** GameSession（构造时无人订阅，事件会丢）。StartFrame 只把 `GameConfig` 写入 `MainController`，然后切场景。`GameView._Ready` 调 `MainController.StartSession(config)` 构造 `GameSession` 并订阅 `EventOccurred`；`GameView._ExitTree` 退订并调 `MainController.EndSession()` 置空。Autoload 不随场景销毁，故必须由 GameView 显式 EndSession 兜底，避免旧 session 残留。
- 未来 SettingView / ReplayView 各自独立场景，同样经 `MainController` 取状态，与本方案兼容。

## 3. 组件

### 3.1 GameConfig（StartFrame → MainController → GameView 的契约）

纯 POCO，定义在 `Quoridor.UI.Logic`：

```
record GameConfig(
    BoardVariant Variant,        // Standard(9x9) | Kid(7x7)
    MatchMode Mode,              // VsAi | HotSeat
    Difficulty AiDifficulty,     // Mode=VsAi 时生效; hot-seat 时忽略
    PlayerId FirstMove)          // 先手方 (P1 | P2)
```

`SeatsBuilder.Build(GameConfig) → IReadOnlyList<IPlayer>`：返回 `[先手座位, 后手座位]`，两个座位都赋 `PlayerId.P1 / P2`（Domain 始终 P1 起手，见 §6）。具体身份由 `FirstMove` + `Mode` 决定：
- `FirstMove=P1`：seats[0]=P1=先手玩家身份，seats[1]=P2=后手玩家身份。
- `FirstMove=P2`：seats[0]=P1=**玩家2**身份（先手），seats[1]=P2=**玩家1**身份（后手）——即 UI 层把"想先手的玩家"放到 P1 座位，使 Domain 的"P1 起手"自然满足。

人/AI 身份：`Mode=VsAi` 时一个座位为 `HumanPlayer`、另一个为 `AiPlayerFactory.Create(...)`；`HotSeat` 时两个都是 `HumanPlayer`。哪个座位是人/AI 由"先手是否为人类玩家"决定（MVP 约定：本地人类始终是"玩家1"，AI 是"玩家2"；先手选择只换座位顺序，不换人/AI 身份归属——即 VsAi 下若选 P2 先手，则 P1 座位=AI、P2 座位=人类，AI 先走）。

> 该处理不触碰 Domain（`GameSetup.Create` 硬编码 `ActivePlayer=P1`，故必须由座位顺序达成先手选择），纯 UI 层换位。需单测覆盖（§9.1）。

### 3.2 StartFrame（简）

`Control` 根场景。控件：
- `OptionButton` 棋盘规格（标准 9×9 / Kid 7×7）
- `OptionButton` 对战模式（人机 / hot-seat）
- `OptionButton` AI 难度（简单 / 中等 / 困难）—— 仅人机模式可见/可选；hot-seat 下隐藏
- `OptionButton` 先手方（玩家 1 / 玩家 2）
- `Button` 开始对局 → 校验后写 `MainController.Config`，`ChangeSceneToFile(GameView)`

无业务逻辑，只产 `GameConfig`。

### 3.3 GameView 场景树

```
GameView (Node3D 根, 脚本 GameViewRoot.cs)
├ DirectionalLight3D        (shadow_enabled=true, 主光, 略俯角, 与相机不同向避免阴影正压)
├ WorldEnvironment          (Environment: 色调映射 + 柔和辉光 + 环境光)
├ Camera3D                  (固定倾斜俯视, 正交; 不可交互旋转)
├ Board3D (Node3D, 脚本 BoardView.cs)
│   ├ BoardPlate (MeshInstance3D, 平板, StandardMaterial3D)
│   ├ Cells (Node3D)
│   │   └ Cell×N (MeshInstance3D + Area3D + CollisionShape3D 用于点击)
│   ├ WallSlots (Node3D)
│   │   └ VerticalSlot×M / HorizontalSlot×M (Area3D + CollisionShape3D, 透明 mesh, input_ray_pickable 可由 BoardView 切换)
│   ├ Walls (Node3D, 动态挂载已落墙的立柱 Mesh)
│   └ Pawns (Node3D)
│       └ Pawn×2 (MeshInstance3D 圆柱/胶囊, 跟随 Cell 世界坐标)
├ PreviewLayer (Node3D)
│   ├ CandidateWallMesh (MeshInstance3D, 半透明, 合法绿/非法红)
│   ├ RouteLines (ImmediateMesh 节点, 每帧重画各棋子最短路线)
│   └ StepLabels (Label3D×N, 步数)
└ CanvasLayer (HUD)
    ├ TopBar (当前回合/模式/先手/双方身份)
    ├ NotationPanel (RichTextLabel, 仅追加 PawnMoved/WallPlaced, 带先手/后手标签)
    ├ WallBudgetBar (双方剩余墙数)
    └ Footer (提示: 终局胜者 / 非法操作原因 / "回到开始页"按钮)
```

> **Godot 节点说明**：
> - 3D 画线用 `ImmediateMesh`（每帧重画，适合预览层高频刷新）；Godot 4 **无 `Line3D` 节点**。步数用 `Label3D`（存在）。
> - `Area3D` 必须有 `CollisionShape3D` 子节点才能被 `input_event`/`mouse_entered` 命中；3D 拾取还需场景有 active `Camera3D`、`Area3D.input_ray_pickable=true`（默认 true）、collision_layer/mask 与默认拾取层匹配。Cell/WallSlot 预制件均含 CollisionShape3D。
> - 在 `input_event` / `EventOccurred` 回调里若要增删节点（如实例化墙立柱 mesh），用 `CallDeferred` 推迟一帧，避免信号栈内改树引发 "null instance" / 迭代器失效；纯属性设置（棋子位置）可直接赋值。

`GameViewRoot.cs`（`Node3D` 根脚本）：
- `_Ready`：从 `MainController` 取 `GameConfig`，调 `StartSession` 构造 `GameSession`，**先**确保 BoardView/HUD/PreviewLayer 子节点就绪（Godot 子节点 _Ready 先于父节点），**再**订阅 `EventOccurred`，最后 `session.Start()`。若起手为 AI，`Start` 会立即同步广播 AI 手事件 → `OnEvent` 在 `_Ready` 栈内触发；故订阅前所有被 `OnEvent` 访问的子节点必须已就绪。可选更稳做法：`CallDeferred(nameof(Start))` 把 Start 推一帧。
- `_ExitTree`：退订、`MainController.EndSession()`。
- **绝不**在 `EventOccurred` 回调里调 `session.Submit`/`Start`（§8 重入纪律）。

`BoardView.cs`（`Board3D` 节点脚本）：
- 持有 `GameSession` 引用与 `BoardLayout`（来自 `Quoridor.UI.Logic`，根据 `BoardConfig.Size` 算出每格世界尺寸、槽位世界坐标、anchor↔slot 映射、行→世界 Y 翻转）。
- `Render(GameState)`：幂等全量刷新——棋子位置、已落墙立柱集合（按 `State.Walls` 对齐，缺的补、多的删，增删用 `CallDeferred`）、回合指示；若 `ActivePlayer.WallsLeft==0` 则禁用所有 WallSlot 的 `input_ray_pickable`（避免无谓触发拒绝）；`State.IsFinished` 时禁用 Cell/WallSlot 输入。
- 输入：`Cell.Area3D.input_event` → 走子；`WallSlot.Area3D.mouse_entered/exited` + `input_event` → 设墙悬浮预览与确认。

### 3.4 Quoridor.UI.Logic（纯 C# UI 逻辑库）

零 Godot 依赖。核心类型（全部不可变 record/struct）：

```
BoardLayout {
  BoardConfig Cfg;
  float CellSize;
  (float X, float Y, float Z) CellToWorld(Cell c);   // 纯数学元组, 调用处转 Godot.Vector3
  Cell? WorldToCell(float x, float z);
  WallPos? SlotToWall(SlotId slot);   // 见 §5
  SlotId? WallToSlot(WallPos wall);   // 反向, 取墙的近边槽(竖墙取下槽/横墙取左槽), 用于预览叠绘定位
  IEnumerable<SlotId> PickableSlots();
}
SlotId { enum Edge { Vertical, Horizontal }, int Col, int Row }  // Row 采用 Domain 约定: 0=南/底, 向上递增
SeatsBuilder.Build(GameConfig) → IReadOnlyList<IPlayer>
RejectReasonText.Of(RejectReason) → string   // 本地化文案
```

> `Vector3` 边界：为保持零 Godot 依赖，Logic 库用 `(float X, float Y, float Z)` 元组；Godot 脚本在调用处转 `Godot.Vector3`。这是"可测纯逻辑"与"Godot 渲染"的硬边界。

## 4. 数据流

### 4.1 开局

```
StartFrame 收集配置 → MainController.Config = gameConfig
  → ChangeSceneToFile(GameView)
  → GameViewRoot._Ready → MainController.StartSession(config)
       构造 GameSession(cfg, SeatsBuilder.Build(config), new GodotAppLogger())
       (确保子节点就绪) session.EventOccurred += OnEvent
       session.Start()   // P1 起手; 若 P1 为 AI 则自动驱动, 人类起手空操作
  → BoardView.Render(session.State)
```

### 4.2 人类走子

```
Cell.Area3D input_event(点击) → BoardView.OnCellClicked(Cell)
  → 构造 MovePawnCommand(Cell)
  → session.Submit(cmd)   // 在输入处理器内调用, 不在 EventOccurred 回调内
```

### 4.3 人类设墙（含辅助模式悬浮预览）

```
WallSlot.Area3D mouse_entered → BoardView.OnSlotHover(SlotId)
  → layout.SlotToWall(slot) → WallPos
  → PreviewService.PoseWall(session.State, wall) → PreviewResult{Legal, Reason, Routes[]}
  → PreviewLayer 叠绘(属性直接设, 结构变更 CallDeferred):
       CandidateWallMesh 显半透明立柱(合法绿/非法红)
       RouteLines(ImmediateMesh) 画各棋子最短路线
       StepLabels(Label3D) 标步数
  → 鼠标移开(mouse_exited) → 清除 PreviewLayer

WallSlot.Area3D input_event(点击) → BoardView.OnSlotClicked(SlotId)
  → layout.SlotToWall(slot) → WallPos
  → session.Submit(PlaceWallCommand(wall))
  // 落墙成功后 WallPlaced 事件触发 OnEvent → Render 清空 PreviewLayer 并加立柱
```

`PoseWall` 只读，不改 `session.State`，符合 §6.2。

### 4.4 事件 → 刷新

`GameViewRoot.OnEvent(IGameEvent e)`：根据事件类型更新 UI。**只改节点视觉，绝不调 `Submit`/`Start`**。

| 事件 | UI 动作 |
|---|---|
| `PawnMoved` | `BoardView.Render(session.State)`（棋子位置幂等刷新）；NotationPanel 追加"先手/后手: 坐标" |
| `WallPlaced` | `BoardView.Render`（加立柱、墙数刷新、清 PreviewLayer）；NotationPanel 追加"先手/后手: 墙位" |
| `WallRejected` / `MoveRejected` | Footer 闪显 `RejectReasonText.Of(Reason)`；状态不变 |
| `TurnPassed` | `BoardView.Render`（回合指示切换） |
| `PlayerWon` | Footer 显示胜者（按 §6 换位映射回"玩家1/玩家2"）；BoardView 高亮胜者棋子；禁用 Cell/WallSlot 输入 |

> **同步广播语义**：`Submit` 内联调用 `DriveAi`，故人类一手 `Submit` 会在同一调用栈内**顺序**广播人类手 + 若干 AI 手事件（`Broadcast` 顺序 Invoke，**非嵌套重入**）。`OnEvent` 因此被顺序多次调用。要求：`OnEvent` 对节点的更新**幂等**（基于最新 `session.State` 重算）。NotationPanel 按"每个 PawnMoved/WallPlaced 事件触发一次追加"即可——每个事件实例只广播一次，无需 cursor 去重；`Rejected`/`TurnPassed` 不追加记谱。
>
> **回到开始页**：Footer 的"回到开始页"按钮在 `PlayerWon` 后出现（终局可见；对局进行中不显示）。点击 → `MainController.EndSession()` → `ChangeSceneToFile("res://Scenes/StartFrame.tscn")`。

### 4.5 AI 驱动

完全由 `GameSession.Submit` / `Start` 内部 `DriveAi` 驱动；UI 不主动触发 AI。AI 手经同一 `EventOccurred` 通道刷新棋盘。MVP 不对 AI 思考做刻意延时（同步广播，AI 手可能瞬间连出）；"AI 思考延迟"延 Plan 5 Setting。届时若引入异步/协程，§8 重入纪律需扩展（Plan 5 处理）。

## 5. 设墙 UX：槽 ↔ WallPos 精确映射

### 5.1 Domain 几何（已核实 `BoardGraph.EdgesOf`）

- `WallPos(Anchor=(c,r), Vertical)`：位于**列 c 与 c+1 之间的竖向凹槽**，阻断两条**横向 passage**：`Between((c,r),(c+1,r))` 与 `Between((c,r+1),(c+1,r+1))`。即竖墙在凹槽中跨行 r 与 r+1 两段。
- `WallPos(Anchor=(c,r), Horizontal)`：位于**行 r 与 r+1 之间的横向凹槽**，阻断两条**纵向 passage**：`Between((c,r),(c,r+1))` 与 `Between((c+1,r),(c+1,r+1))`。即横墙在凹槽中跨列 c 与 c+1 两段。
- 锚点边界（`WallLegality`）：`Anchor.Col, Anchor.Row ∈ [0, MaxIndex-1] = [0, Size-2]`。

### 5.2 UI 槽位约定

`SlotId.Row` **采用 Domain 行约定**：0 = 南/底，向上递增（与 `GameSetup` 的 P1 起 `Cell(mid,0)` 目标 North 一致）。屏幕渲染时由 `BoardLayout.CellToWorld` 把 row 翻转为世界 Y（row 0 画在棋盘近端/屏幕下方）。**槽坐标与 Domain anchor 同构，映射为恒等**。

> 用户最初表述为"本槽 + 下方槽"——那是屏幕视角（row 0 在顶、向下递增）。本 spec 统一采用 Domain 行约定（row 0 在底、向上递增），故 prose 为"上方槽"；功能等价（墙向远离近边的方向延伸）。实现以本节映射为准。

### 5.3 映射

- **竖向槽 `SlotId(Vertical, c, r)`**：列 c 与 c+1 之间凹槽中、行 r 那一段。
  - 映射：`SlotToWall → WallPos(Anchor=(c, r), Vertical)`，即"本槽 + 上方槽(r+1)"合成竖墙。
  - 可触发范围：`r ∈ [0, Size-2]`（顶排竖槽 r=Size-1 无上方槽，不可触发）。例：Size=9 → r∈[0,7]，r=8 不可触发；Size=7 → r∈[0,5]，r=6 不可触发。
  - `c ∈ [0, Size-2]`（凹槽本身只存在于相邻列之间）。
- **横向槽 `SlotId(Horizontal, c, r)`**：行 r 与 r+1 之间凹槽中、列 c 那一段。
  - 映射：`SlotToWall → WallPos(Anchor=(c, r), Horizontal)`，即"本槽 + 右侧槽(c+1)"合成横墙。
  - 可触发范围：`c ∈ [0, Size-2]`（最右列横槽 c=Size-1 无右槽，不可触发）。
  - `r ∈ [0, Size-2]`。

性质：每个可触发槽 ↔ 唯一 `WallPos`，无歧义、无需"旋转"操作。槽类型（竖/横凹槽段）隐式决定朝向。`Quoridor.UI.Logic.BoardLayout.SlotToWall` 实现该映射并单测覆盖（含边界不可触发断言）；`WallToSlot` 反向取墙的近边槽（竖墙取下槽 r、横墙取左槽 c）用于预览叠绘定位，单测覆盖往返一致。

## 6. 先手方处理（纯 UI 层换位，不改 Domain）

`GameSetup.Create` 末行硬编码 `ActivePlayer = PlayerId.P1`，`GameSession.Start`/`DriveAi` 从 `State.ActivePlayer` 起驱动——故 Domain 永远 P1 起手，无法直接表达"P2 先手"。MVP 处理：**把"想先手的玩家"放到 P1 座位**。

- `SeatsBuilder.Build(config)` 返回 `[先手座位(=P1), 后手座位(=P2)]`。
- `FirstMove=P1`：P1 座位 = 玩家1（先手），P2 座位 = 玩家2（后手）。
- `FirstMove=P2`：P1 座位 = 玩家2（先手），P2 座位 = 玩家1（后手）。
- VsAi 下：人类=玩家1、AI=玩家2 为固定身份；`FirstMove=P2` 时 P1 座位=AI（先手，AI 自动起手）、P2 座位=人类（后手）。`session.Start()` 即驱动 P1=AI 先走。

HUD 显示：TopBar/NotationPanel/Footer 一律按"玩家1/玩家2"显示，通过 `SeatMap`（P1→玩家1还是玩家2，由 `FirstMove` 决定）做 Domain PlayerId → 显示玩家的映射。终局 `PlayerWon(Who)` 同样经 `SeatMap` 映射回"玩家1/玩家2"。

需单测：`SeatsBuilder.Build` 在 `(Mode=VsAi, FirstMove=P1)` / `(VsAi, P2)` / `(HotSeat, P1)` / `(HotSeat, P2)` 四组合下，座位 `Id` 顺序与 `IsHuman` 身份正确；`SeatMap` 映射双向正确。

## 7. 渲染与光影

- **相机**：`Camera3D`，绕 X 轴约 -55° 俯视，`Projection=Orthogonal`（等距感强、无近大远小变形；若预览效果差可改低透视，低风险可逆），`Size` 适配棋盘。固定，不可交互旋转。中心由 `BoardLayout` 算出的包围盒定。
- **主光**：`DirectionalLight3D`，`ShadowEnabled=true`，方向略斜（与相机不同向，避免阴影正压棋子正下）。墙立柱与棋子向棋盘投实时阴影——"2.5D 有光影"的核心。
- **环境**：`WorldEnvironment` + `Environment`：`Tonemap`（Filmic）、`Glow` 低强度、`AmbientLight`/`Sky` 柔和填充。质感灵魂。
- **材质**：`StandardMaterial3D`：棋盘板低粗糙度微反射、墙立柱木色 Roughness 中、棋子圆柱高 gloss。Kid 主题后续替换为贴图/Sprite3D。
- **HUD**：`CanvasLayer`（独立 2D 层，不参与 3D 光影），放 TopBar/NotationPanel/WallBudgetBar/Footer。主题用 Godot `Theme` 资源统一管字号/颜色/按钮样式（MVP 一个 ClassicTheme，Kid 主题 Plan 6 加）。

## 8. 错误处理与结转项落地

| 结转项 | MVP 处理 |
|---|---|
| GameSession 重入未保护 | **UI 硬纪律**：`EventOccurred` 回调只改节点视觉，绝不调 `Submit`/`Start`。所有 `Submit` 调用只在输入处理器（点击）或 `MainController.StartSession` 内。spec 与代码注释双重标注。`OnEvent` 幂等（基于最新 `State` 全量刷新）；结构变更用 `CallDeferred`。 |
| ReplayController StepBack/GoTo 非法手丢 cursor | Replay 推迟 Plan 6，本计划不涉及。 |
| IAppLogger 模板占位符待真实 logger | 注入 `GodotAppLogger : IAppLogger`。Application 现有调用全部用**命名占位符**（如 `"cfg={Variant} players={N}"`），标准 `string.Format` 遇命名 token 会抛 `FormatException`。故 `GodotAppLogger` **自行实现命名占位符→位置参数替换**（按 `args` 顺序填充 `{Name}` token，支持 `{{`/`}}` 转义），再 `GD.Print`/`GD.PushWarning`/`GD.PushError`。该替换逻辑放 `Quoridor.UI.Logic`（纯 C# 可单测）。 |
| Submit 的 DriveAi 硬编码 maxPlies | `DefaultMaxPlies=1000` 是防失控上限，非搜索深度；AI 强度由 `Difficulty` 控制（已就绪）。MVP 不改；"AI 难度"暴露给用户即满足需求。 |

UI 侧错误展示：
- `WallRejected`/`MoveRejected`：Footer 闪显 `RejectReasonText.Of(Reason)`（本地化映射表，纯 C# 可单测）。
- 当前玩家墙数=0：`BoardView.Render` 禁用 WallSlot 的 `input_ray_pickable`，避免无谓拒绝。
- 终局后输入禁用：`State.IsFinished` 时 Cell/WallSlot 输入全部禁用，Footer 显示"回到开始页"。
- Quoridor 无平局；hot-seat 下若用户死循环属用户行为，MVP 不设回合上限（`DefaultMaxPlies` 只限 AI 驱动，人手不限）。

## 9. 测试策略

Domain/Application 已有 103 测试，UI 不重复测其逻辑。UI 侧可测范围：

### 9.1 纯 C# 单测（`Quoridor.UI.Logic.Tests`，net10.0，零 Godot）

- `BoardLayout.SlotToWall`：竖/横槽 → `WallPos` 映射正确；边界槽（顶排竖槽 r=Size-1 / 最右列横槽 c=Size-1）返回 null（不可触发）；Standard(9) 与 Kid(7) 两种 Size 均覆盖；可触发范围 `[0,Size-2]` 断言。
- `BoardLayout.WallToSlot`：与 `SlotToWall` 往返一致（对可触发槽）。
- `BoardLayout.WorldToCell` / `CellToWorld`：往返一致；越界返回 null。
- `SeatsBuilder.Build` + `SeatMap`：`(VsAi,P1)` / `(VsAi,P2)` / `(HotSeat,P1)` / `(HotSeat,P2)` 四组合的座位 Id 顺序、IsHuman 身份、Domain PlayerId→显示玩家映射双向正确。
- `RejectReasonText.Of`：每个 `RejectReason` 枚举值有非空文案。
- `GodotAppLogger` 的命名占位符替换：`{Name}` 按顺序填充、`{{`/`}}` 转义、参数数量不匹配时不抛（降级原样输出 + 标记）。

### 9.2 UI 集成（手动 + 冒烟）

- Godot 场景级测试在 MVP 不上自动化（成本高、收益低）。改为：`demo/Quoridor.Demo`（已存在的 AI 自对弈控制台）继续作为后端回归；UI 侧提供一份**手动验收清单**：
  1. 人机标准 9×9，玩家1先手，走完一局至胜。
  2. 人机 Kid 7×7，玩家2先手（AI 先走），验证换位。
  3. hot-seat 两人交替走子与设墙。
  4. 设墙悬浮预览：合法(绿)+路线+步数；非法(红)（如切断玩家路径）。
  5. 墙数耗尽后墙槽不可拾取。
  6. 终局提示胜者 + "回到开始页"可循环。
  7. 辅助模式预览在 mouse_exit 后清除。
- 关键不变量用断言式日志：`GodotAppLogger` 把 Error/Warning 同时写入内存 ring buffer；手动验收结束退出时 dump，便于事后核查（避免人眼漏看 `GD.PushWarning`）。

### 9.3 回归

`dotnet test`（Domain + Application + UI.Logic.Tests）全绿为合入门槛。Godot 项目 `dotnet build` 通过为构建门槛。`godot-mono --headless --quit` 能加载项目无报错为运行门槛。

## 10. 项目结构

```
src/Quoridor.UI.Logic/         (新增, 纯 C# net10.0, 零 Godot)
  Quoridor.UI.Logic.csproj     (ProjectRef: Application → Domain)
  BoardLayout.cs, SlotId.cs, GameConfig.cs, SeatsBuilder.cs, SeatMap.cs,
  RejectReasonText.cs, NamedPlaceholder.cs   (logger 占位符替换)
src/Quoridor.UI/               (新增, Godot 4.7 mono 项目)
  project.godot
  Quoridor.UI.csproj           (ProjectRef: Application + UI.Logic)
  Scenes/StartFrame.tscn, GameView.tscn, board/Cell.tscn, WallSlot.tscn, Pawn.tscn
  Scripts/GameViewRoot.cs, BoardView.cs, MainController.cs, GodotAppLogger.cs
  Themes/ClassicTheme.tres
  Assets/  (空, 待 Plan 6 填 Kid 资产)
tests/Quoridor.UI.Logic.Tests/ (新增, net10.0)
  Quoridor.UI.Logic.Tests.csproj (ProjectRef: UI.Logic)
```

`Quoridor.slnx` 加入三个新项目（src 两个 + tests 一个）。

## 11. 待确认 / 假设

- MVP 仅 2 人对局（人机 / hot-seat）。4 人推迟。（假设，已默认。）
- 先手方 = P1/P2 语义换位（§6），不改 Domain。（已核实 GameSetup 硬编码 P1 起手。）
- 相机正交；如预览效果差可改低透视。（低风险可逆。）
- AI 思考无刻意延时；棋子瞬移无过渡动画。二者均延 Plan 5。（假设。）
- `Quoridor.UI.Logic` 独立成库（而非塞进 Godot 项目）以保证可测性。（架构决策，已核实依赖链无环。）
- VsAi 下人类=玩家1、AI=玩家2 为固定身份；先手只换座位顺序。（假设，可调。）
