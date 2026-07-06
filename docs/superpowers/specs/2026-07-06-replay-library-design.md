# 回放预置棋局库 设计

> 日期: 2026-07-06
> 状态: 设计待审
> 关联: `docs/PROJECT_GUIDE.md` §6 后续 Plan 5(Replay)

## 1. 目标

提供"观战 AI vs AI 棋局"能力:预置 18 局完整自对弈记谱,玩家加载后用前进/后退/跳转逐步观察 AI 策略,避免实时 AI 长搜索卡顿。

**核心洞察**:AI 决策固定但耗时(Hard 9×9 单步 ~15s)。把"演算"与"观察"分离——提前演算存档,回放只读不演算。`ReplayController` 已具备前进/后退/跳转,本功能 = 预置棋局库 + 回放 UI 装配,核心逻辑零新开发。

## 2. 范围

- **9 难度组合**:先手方难度(Easy/Medium/Hard)× 后手方难度(Easy/Medium/Hard)。P1 固定先手(先手方=P1),P2 后手。9 组合本身已涵盖先后手关系——"Easy vs Hard"与"Hard vs Easy"是两局不同记谱。
- **2 变体**:Standard(9×9)、Kid(7×7)。
- **共 18 局**。

## 3. 非目标

- 运行时按需演算棋局(违反"提前演算")。
- 用户自选难度实时对抗(已有 VsAi 模式)。
- 4 人模式棋局。
- 棋局编辑/导入(仅消费预置库)。

## 4. 架构(三层依赖不变)

```
UI.Logic:  ReplayLibrary(纯 C# 常量 + 元数据)  ──可单测
Application: ReplayController(已存在, 零改)
UI:        GameViewRoot 回放分支 + HUD 控制条
demo:      生成器扩展(--gen-replays, 一次性工具)
```

## 5. 组件

### 5.1 ReplayEntry / ReplayLibrary(`src/Quoridor.UI.Logic/ReplayLibrary.cs`)

```csharp
public sealed record ReplayEntry(
    string Name,           // "Standard · Easy vs Medium"
    BoardVariant Variant,
    Difficulty P1Diff,     // 先手方难度
    Difficulty P2Diff,     // 后手方难度
    PlayerId Winner,
    int Plies,             // 总手数
    string Notation);      // 完整记谱串

public static class ReplayLibrary
{
    public static IReadOnlyList<ReplayEntry> All { get; }  // 18 条
}
```

18 条目由生成器产出。`Name` 标签格式 `{Variant} · {P1Diff} vs {P2Diff}`。

### 5.2 生成器(`demo/Quoridor.Demo/Program.cs` 加 `--gen-replays`)

- 遍历 9 难度组合 × 2 变体 = 18 局。
- 每局:`new GameSession(board, [AiPlayer(P1,P1Diff), AiPlayer(P2,P2Diff)], autoDriveAi: true)` → `Start()` 同步驱动到终局 → `Export()` 取记谱串 → 记 `Winner`/`Plies`。
- 汇总产出 `src/Quoridor.UI.Logic/ReplayLibrary.cs`(手写文件,记谱串嵌为 C# 字符串常量)。
- 一次性后台跑(~1h,Hard 局慢;并行 Minimax 已在)。

### 5.3 GameConfig 扩展(`src/Quoridor.UI.Logic/GameConfig.cs`)

```csharp
public enum MatchMode { VsAi, HotSeat, Replay }  // 加 Replay

public sealed record GameConfig(
    BoardVariant Variant,
    MatchMode Mode,
    Difficulty AiDifficulty,
    PlayerId FirstMove,
    ReplayEntry? Replay);  // 新增: Replay 模式非 null, 其他模式 null
```

Replay 模式下 `Variant` 取自 `Replay.Variant`;`AiDifficulty`/`FirstMove` 忽略。

### 5.4 StartFrame(`Scripts/StartFrameView.cs`)

- 模式 `OptionButton` 加"回放"项。
- 选中回放 → 显示棋局 `OptionButton`(18 项,标签 `{Name} · P1胜/P2胜`),隐藏难度/先手选项。
- `OnStart`: `Mode=Replay`,`Replay=ReplayLibrary.All[selected]`。

### 5.5 GameViewRoot 回放分支(`Scripts/GameViewRoot.cs`)

`_Ready`:
- `Mode==Replay` → **不 `StartSession`**;构造 `ReplayController(BoardConfig, 2, replay.Notation)`;`_board.Init(ctrl, replay.Current)`(重载,传初始 state 建棋子);`Render(replay.Current)`;HUD 回放控制条可见;**不订阅 Session 事件,不 KickAi**。
- 其他模式 → 现有路径不变(调 `Init(ctrl)`,内部用 `ctrl.Session.State`)。

回放按钮(经 HUD 事件):
- ⏮ Reset → `replay.Reset()` → Render
- ⬅ StepBack → `replay.StepBack()` → Render
- ➡ StepForward → `replay.StepForward()` → Render
- ⏭ GoTo 末 → `replay.GoTo(replay.Total)` → Render

每次步进:`_board.Render(replay.Current)` + `_hud` 刷新步数/当前手。

### 5.6 HUD(`Scripts/HudView.cs`)

- 回放控制按钮(仅 Replay 模式可见):⏮ ⬅ ➡ ⏭ + "步数 N/M"。
- 状态行显示:`{Name} · 第 N/M 手 · 轮到 玩家X` / 终局 `★ 玩家X 获胜!`。
- 投降/AI 思考提示在 Replay 模式隐藏。

## 6. 数据流

1. StartFrame 选回放 + 选棋局 → `GameConfig(Mode=Replay, Replay=entry)` → `ChangeSceneToFile(GameView.tscn)`
2. `GameView._Ready`: `Mode==Replay` → `ReplayController(notation)` → `Render(Current)` + 控制条
3. 点 ➡ → `StepForward` → `Render(Current)` → 刷新步数
4. ⏭ → `GoTo(Total)` → 终局局面

## 7. 错误处理

- 记谱串非法:`ReplayController` 构造时 `NotationService.Decode` 抛 `NotationParseException`。生成器产出的记谱串由自对弈合法走子生成,保证合法;测试锁定。
- 回放模式不调 `GameSession`,无 AI 异常路径,无重入风险。

## 8. 测试(UI.Logic,纯 C# 可单测)

`ReplayLibraryTests`:
- `All.Count == 18`。
- 9 难度组合 × 2 变体全覆盖(去重校验)。
- 每条:`NotationService.Decode(entry.Notation, cfg)` 成功;`ReplayController(cfg, 2, notation).GoTo(Total)` 后 `IsFinished` 且 `Winner==entry.Winner`;`Cursor==entry.Plies`。

SeatsBuilder/GameSession/ReplayController 既有测试不变。

UI(Godot 脚本)无单测,靠手动验收:18 局可加载、前进/后退/跳转/重置正常、终局显示正确。

## 9. 生成成本与执行

- Hard 局每手 ~15s(并行后),一局 30-60 手 → 7-15min/局。含 Hard 的组合 10 局(Standard 5 + Kid 5)。
- 总计 ~1h,后台跑,一次性。生成器是临时工具,产物 `ReplayLibrary.cs` 提交 git。

## 10. 与既有代码的关系

- `ReplayController`:零改(已支持 StepForward/Back/GoTo/Reset)。
- `NotationService`:零改(Encode/Decode 已有)。
- `GameSession`:零改(生成器用 `autoDriveAi:true` 同步路径)。
- `GameViewRoot`:加 `Mode==Replay` 分支,现有 VsAi/HotSeat 路径不动。
- `HudView`:加回放控制按钮 + 状态方法,现有方法保留。
- `BoardView`:`Init` 加重载 `Init(MainController ctrl, GameState initial)`(回放用,不依赖 Session);`Render(state)` 零改(幂等,回放复用)。
