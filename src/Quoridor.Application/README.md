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
