# Quoridor 项目

桌面客户端版 Quoridor(墙棋)。Godot 4.7 mono + C# / .NET 10 + xUnit。中文协作。

## 现状(2026-07-07)

Plan 1-4 + 体验改进 + 回放功能 + 验收收口 + UI fix 已 FF 合入 `master`(HEAD `a7e1a6b`,**已推送 origin**):
- Plan 1 Domain Core(纯 C# 不可变 GameState + 规则 + BFS + 代数记谱)— 74 测试(含投降 4)
- Plan 2 AI(IQuoridorAi + Greedy + Minimax Alpha-Beta)— 合入 Domain 测试
- Plan 3 Application(GameSession/PreviewService/ReplayController/IAppLogger + AiPlayerFactory)— 41 测试(含异步驱动 6)
- Plan 4 Godot UI MVP(StartFrame + GameView 2.5D 棋盘 + 人机/hot-seat + 设墙预览)— 24 测试,验收项 1-7 全通过
- 体验改进(2026-07-06):AI 异步化(GameSession `autoDriveAi=false` + `PeekAiProposal` 后台线程 + `_Process` 主线程消费,不阻塞渲染)+ Minimax 根级并行(`Parallel.For` + 共享 alpha)+ 大字号状态提示 + 投降(`SurrenderCommand`/`PlayerSurrendered`)
- 回放功能(2026-07-06):预置 18 局 AI vs AI 棋局库(`ReplayLibrary`,9 难度组合 × 2 变体,`demo --gen-replays` 一次性演算)+ 回放 UI(⏮⬅➡⏭ 控制条,复用 `ReplayController`,零改 Application/Domain)
- Plan 4 验收收口(2026-07-06):PreviewService 补 Kid 变体覆盖 + 精确步数 + 忽略墙数 4 测试;`demo --acceptance` 半自动回归(换位断言 + Kid 终局流 + 预览合法非法);手动验收清单 `docs/superpowers/acceptance/2026-07-06-plan4-acceptance-checklist.md` 7 项已全部人工验收通过
- UI fix(2026-07-06):墙槽拾取区从 2 格宽改 1 格宽(`BoardLayout.SlotPickCenter`,相邻槽相切不重叠)修复设墙预览随鼠标进入方向左右抖动;回放控制条 ⬅ 按钮加 U+FE0F 与 ⏮/➡/⏭ 统一 emoji 呈现

**测试**:`dotnet test Quoridor.slnx` → Domain 74 + Application 45 + UI.Logic 49 = 168 全绿。
**运行游戏**:`cd src/Quoridor.UI && godot-mono --path .`(不是 `godot`,须 mono 版)。
**构建**:`dotnet build src/Quoridor.UI/Quoridor.UI.csproj`(Godot 项目须 pin `Sdk="Godot.NET.Sdk/4.7.0"`)。

详见 `docs/PROJECT_GUIDE.md`(架构 + 代码导览 + .NET/Godot 学习路径 + 踩坑)。spec 在 `docs/superpowers/specs/`,计划在 `docs/superpowers/plans/`。

## 架构(三层单向依赖)

`UI(Godot) → Application → Domain`。Domain 零 Godot 依赖(纯 C#,可单测)。`Quoridor.UI.Logic` 是纯 C# 桥接库(坐标映射/座位/文案/logger 占位符,可单测,零 Godot)。`Quoridor.UI` 是 Godot 项目(薄 Node 脚本,程序化构建子节点,无手写 .tscn uid)。

## 关键约束/坑(新 session 必读)

- **Godot 4.7 mono 内嵌 .NET 8 运行时,但 `GodotPlugins.runtimeconfig.json` 用 `rollForward:LatestMajor`**,前滚到本机 .NET 10 → **net10.0 程序集可加载,无需降 TFM**。曾误判 TFM 不匹配,实为 `project.godot` 的 `[dotnet] project/assembly_name` 须与 csproj 产物名一致(`Quoridor.UI`,不是 `Quoridor`)。
- **CLI 无法触发 Godot C# 构建**(`--build-solutions`/`--editor --quit` 均不触发 MSBuild);但 `dotnet build` 产出 DLL + assembly_name 一致时,headless/runtime 直接能加载,无需 Godot 自建。
- **Domain/Application 不加日志**(纯逻辑层);日志在 Application 的 `IAppLogger` 落实,Godot 端由 `GodotAppLogger` 桥到 `GD.Print/PushWarning/PushError`。
- **GameState 是 sealed record class(引用类型)**,`GameState?` 无 `.Value`,取值用 `r.State!`。
- **墙几何**:水平墙 anchor(c,r) 阻 (c,r)-(c,r+1) 与 (c+1,r)-(c+1,r+1);垂直墙阻 (c,r)-(c+1,r) 与 (c,r+1)-(c+1,r+1);同 anchor 不同朝向="+"字交叉非法(T 字合法)。
- **坐标**:Cell (c,r) 世界中心 = `((c+0.5)*s, 0, (MaxIndex-r+0.5)*s)`;墙中心在格交点 `((c+1)*s, (MaxIndex-r)*s)`,集中 `BoardLayout.WallCenter`。
- **EventOccurred 回调内禁止重入 Submit/Start**(GameSession 无重入保护)。

## 后续(未立项)

Plan 4 验收项 1-7 已全部关闭 / Plan 5 剩余(Setting 面板;Replay 已于 2026-07-06 完成,Setting 倾向轻量塞 StartFrame 而非独立场景) / Plan 6(Kid主题+4人+动画) / UI 美化(Container+Anchor 布局+真实 Theme,替代硬编码坐标)。

## 关键约束/坑(2026-07-06 新增)

- **AI 异步化**:`GameSession(autoDriveAi:false)` 时 Start/Submit 不自动驱动;UI 用 `PeekAiProposal`(线程安全只读)在 `Task.Run` 后台跑,`ConcurrentQueue` + `_Process` 主线程消费 `Submit`。`EventOccurred` 回调内仍禁重入 Submit/Start。
- **Minimax 并行**:`MinimaxAi.Choose` 根节点 `Parallel.For` + `Volatile.Read(bestScore)` 作共享 alpha 恢复剪枝。Medium 加速 3-4x;Hard 基本持平(alpha-beta 串行剪枝本质 + GC 压力限制,要真正加速需 YBWC 或置换表/动作裁剪)。
- **回放模式**:`GameViewRoot._Ready` 检测 `Mode==Replay` → 不 `StartSession`,改用 `ReplayController`;`BoardView.Init(ctrl, initial)` 重载不依赖 Session;回放分支不订阅 `EventOccurred`/`CellClicked`,不 `KickAi`。
- **生成器 worktree 坑**:`demo --gen-replays` 的 `FindRepoRoot` 须同时检查 `.git` 目录与文件(worktree 的 `.git` 是文件,否则向上找到主仓库写错位置)。
- **Area3D 拾取区禁重叠**:多个 `Area3D`(`input_ray_pickable=true`)拾取区空间重叠时,鼠标在重叠区两区都触发 `mouse_entered`,last-write-wins,胜出者取决于鼠标进入方向(非确定,易误判为浮点边界 bug)。相邻拾取区须相切不重叠(中心间距=各区宽度);拾取中心几何放纯 C# `BoardLayout.SlotPickCenter` 单测非重叠契约。墙槽曾因 2 格宽拾取区在整段格上沿重叠致设墙预览左右抖动(2026-07-06 fix)。
