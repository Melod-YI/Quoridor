# Quoridor 项目

桌面客户端版 Quoridor(墙棋)。Godot 4.7 mono + C# / .NET 10 + xUnit。中文协作。

## 现状(2026-07-03)

四份计划分层,Plan 1-4 已 FF 合入 `master`(HEAD `f7e6758`,**未推送 origin**):
- Plan 1 Domain Core(纯 C# 不可变 GameState + 规则 + BFS + 代数记谱)— 70 测试
- Plan 2 AI(IQuoridorAi + Greedy + Minimax Alpha-Beta)— 合入 Domain 测试
- Plan 3 Application(GameSession/PreviewService/ReplayController/IAppLogger + AiPlayerFactory)— 35 测试
- Plan 4 Godot UI MVP(StartFrame + GameView 2.5D 棋盘 + 人机/hot-seat + 设墙预览)— 24 测试,验收项1通过

**测试**:`dotnet test Quoridor.slnx` → Domain 70 + Application 35 + UI.Logic 24 = 129 全绿。
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

验收项2-7 / Plan 5(Setting+Replay) / Plan 6(Kid主题+4人+动画) / UI 美化(Container+Anchor 布局+真实 Theme,替代硬编码坐标)。
