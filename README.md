# Quoridor(墙棋)

桌面客户端版 Quoridor。Godot 4.7 mono + C# / .NET 10 + xUnit。中文协作。

标准 9×9 / Kid 7×7 双变体,人机(AI 三档难度)/ 双人 hot-seat / AI vs AI 回放,设墙悬浮预览(合法/非法 + 各棋子最短路线步数),代数记谱导出,投降。

> 三层单向依赖 `UI(Godot) → Application → Domain`,Domain 零 Godot 依赖、纯 C# 可单测。

## 现状

Plan 1-4 + 体验改进 + 回放 + 验收收口 + UI fix 已合入 `master`(168 测试全绿,验收项 1-7 全通过)。详见 [CLAUDE.md](CLAUDE.md) 现状节与 [docs/PROJECT_GUIDE.md](docs/PROJECT_GUIDE.md)。

## 快速开始

**环境**:Godot 4.7 **mono** 版(GodotSharp,不是普通 `godot`)、.NET 10 SDK。

```bash
# 运行游戏
cd src/Quoridor.UI && godot-mono --path .

# 全量测试(168)
dotnet test Quoridor.slnx

# 仅构建 Godot 项目(须 pin Godot.NET.Sdk 4.7.0)
dotnet build src/Quoridor.UI/Quoridor.UI.csproj

# AI 自对弈控制台 demo(ASCII 棋盘, Kid 7×7)
dotnet run --project demo/Quoridor.Demo -- easy medium

# 半自动验收回归(换位断言 + Kid 终局流 + 预览合法非法, 退出码 0/1)
dotnet run --project demo/Quoridor.Demo -- --acceptance

# 重新演算预置 18 局回放库(写入 ReplayLibrary.cs, ~1h)
dotnet run --project demo/Quoridor.Demo -- --gen-replays
```

> **坑**:CLI 无法触发 Godot C# 构建(`--build-solutions`/`--editor --quit` 都不触发 MSBuild);但 `dotnet build` 产出 DLL + `project.godot` 的 `assembly_name` 与产物名一致(`Quoridor.UI`)时,headless/runtime 直接加载。曾误判 TFM 不匹配,实为 assembly_name 拼写问题。net10.0 可加载(`GodotPlugins.runtimeconfig.json` 用 `rollForward:LatestMajor` 前滚到本机 .NET 10)。

## 架构(三层单向依赖)

```
UI(Godot) → Application → Domain
```

| 层 | 项目 | 职责 | 测试 |
|----|------|------|------|
| Domain | `src/Quoridor.Domain` | 纯 C# 不可变 `GameState` + 命令/事件 + 规则 + BFS 路径/可达性 + Modern Algebraic Notation;零 Godot 依赖 | 74 |
| Application | `src/Quoridor.Application` | `GameSession`(事件驱动)/`PreviewService`(设墙预览)/`ReplayController`/`IPlayer`+`AiPlayerFactory`/`IAppLogger`;纯 C# | 45 |
| UI.Logic | `src/Quoridor.UI.Logic` | 纯 C# 桥接库:坐标映射 `BoardLayout`/座位 `SeatsBuilder`+`SeatMap`/文案 `RejectReasonText`/logger 占位符/`ReplayLibrary`;零 Godot | 49 |
| UI | `src/Quoridor.UI` | Godot 项目:薄 Node 脚本(`StartFrameView`/`GameViewRoot`/`BoardView`/`PreviewLayerView`/`HudView`/`MainController` autoload),程序化构建子节点,无手写 .tscn uid | (Godot 层不单测) |

Domain/Application 不加日志;日志在 Application 的 `IAppLogger` 落实,Godot 端由 `GodotAppLogger` 桥到 `GD.Print/PushWarning/PushError`。

## 关键设计

- **AI 异步化**:`GameSession(autoDriveAi:false)` 时 Start/Submit 不自动驱动;UI 用 `PeekAiProposal`(线程安全只读)在 `Task.Run` 后台跑,`ConcurrentQueue` + `_Process` 主线程消费 `Submit`,不阻塞渲染。`EventOccurred` 回调内仍禁重入 Submit/Start。
- **Minimax 并行**:`MinimaxAi.Choose` 根节点 `Parallel.For` + `Volatile.Read(bestScore)` 作共享 alpha 恢复剪枝。Medium 加速 3-4x;Hard 基本持平。
- **回放**:预置 18 局 AI vs AI 棋局库(`ReplayLibrary`,9 难度组合 × 2 变体),回放 UI(⏮⬅➡⏭ 控制条)复用 `ReplayController`,零改 Application/Domain。
- **墙几何**:水平墙 anchor(c,r) 阻 (c,r)-(c,r+1) 与 (c+1,r)-(c+1,r+1);垂直墙阻 (c,r)-(c+1,r) 与 (c,r+1)-(c+1,r+1);同 anchor 不同朝向="+"字交叉非法(T 字合法)。
- **设墙预览**:`PreviewService.PoseWall` 只读算(临时叠加墙 + 各棋子 BFS 最短路线 + 合法性),不改真实状态;预览忽略墙数(墙耗尽禁拾由 `BoardView.Render` 另管)。
- **墙槽拾取**:`BoardLayout.SlotPickCenter` 把每个槽拾取区定为 1 格宽、居中墙起始格近边,相邻槽相切不重叠(避免 2 格宽重叠致预览随鼠标进入方向左右抖动)。

## 项目结构

```
src/
  Quoridor.Domain/         纯 C# 规则+状态+路径+记谱
  Quoridor.Application/    GameSession/PreviewService/ReplayController/Seats/Logging
  Quoridor.UI.Logic/       纯 C# 桥接(BoardLayout/SeatsBuilder/SeatMap/ReplayLibrary/...)
  Quoridor.UI/             Godot 项目(Scripts/ Scenes/ Themes/ project.godot)
tests/
  Quoridor.Domain.Tests/   74
  Quoridor.Application.Tests/   45
  Quoridor.UI.Logic.Tests/      49
demo/
  Quoridor.Demo/           控制台:AI 自对弈 + --gen-replays + --acceptance
docs/
  PROJECT_GUIDE.md         架构 + 代码导览 + .NET/Godot 学习路径 + 踩坑
  superpowers/specs/       设计 spec
  superpowers/plans/       实现计划(任务级 checkbox)
  superpowers/acceptance/  手动验收清单
```

## 执行方法论

每个 Plan 一棵 git worktree 隔离,superpowers skill 体系(brainstorming → writing-plans → subagent-driven-development → finishing-a-development-branch),TDD(Domain/Application/UI.Logic 纯逻辑层全 TDD;Godot 脚本靠 `dotnet build` + 手动验收)。spec/plan 经对抗式自审,完成后 FF 合并回 master 并清理 worktree。

## 后续(未立项)

- **Plan 5 剩余**:Setting 面板(Replay 已完成;Setting 倾向轻量塞 StartFrame 而非独立场景,与 UI 美化重叠)
- **Plan 6**:Kid 主题资产(老鼠/奶酪)+ 4 人模式 + 动画
- **UI 美化**:`Container`/`Anchor` 重做布局(替代硬编码坐标)+ 真实 `Theme`(字号/配色/间距)+ 棋子/墙材质光影 + 悬停高亮 + 走子动画
- **AI 算法优化**:置换表/动作裁剪/YBWC 真并行(提升 Hard)

## 许可

私有项目(暂未开源)。
