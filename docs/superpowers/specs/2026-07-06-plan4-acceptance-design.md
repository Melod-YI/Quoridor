> 日期: 2026-07-06
> 状态: 设计待审
> 关联: `docs/PROJECT_GUIDE.md` §6 后续(验收项 2-7)、`2026-07-02-quoridor-ui-design.md` §9.2 手动验收清单

## 1. 目标

把 Plan 4 验收项 2-7 真正收口。审计显示 7 项的 UI/Application 代码均已就位,但只过了第 1 项;逻辑层测试有局部缺口,5 项视觉/交互行为从未走过手动验收。本设计不做新功能,只做**验证 + 补漏 + 文档化**,低风险。

## 2. 范围

### 2.1 本次交付(三层)

- **(a) 逻辑层测试补强**:`PreviewServiceTests.cs` 新增 4 条 xUnit 测试
- **(b) 半自动回归**:`demo/Quoridor.Demo` 加 `--acceptance` 子命令,dump 两场景事件流 + PASS/FAIL
- **(c) 手动验收清单**:新文件 `docs/superpowers/acceptance/2026-07-06-plan4-acceptance-checklist.md`,7 项带操作步骤 + 期望视觉 + 期望日志信号

### 2.2 不做(YAGNI)

- 不做独立 Setting 面板(已确认推迟;其内容若要做,轻量塞 StartFrame 比独立场景合适,与 UI 美化计划重叠,不在本次)
- 不碰 Domain/Application 生产代码(审计未发现 bug)
- 不做 Godot 场景级 UI 自动化(spec §9.2 明确不上,成本高收益低)
- 不重复 SeatsBuilder/GameSession 已有测试(四组合 + 41 测试已覆盖)
- 不引入 AI 思考延时(用户确认无业务需要)

## 3. 验收项 2-7 审计结论

| # | 验收项 | 逻辑层测试 | UI 实现 | 本次处理 |
|---|--------|-----------|---------|---------|
| 1 | 人机标准 9×9 P1先手 | ✅ | ✅ | 已过,不动 |
| 2 | 人机 Kid 7×7 P2先手换位 | SeatsBuilder(VsAi,P2)+SeatMap(P2)✅;Kid 变体 PreviewService 未覆盖 | BoardConfig.Kid ✅ | (a) 补 PreviewService Kid;(b) 场景1;(c) 手动 |
| 3 | hot-seat 两人交替 | HotSeat 两人类 ✅ | ✅ | (c) 手动(逻辑层无"交替"概念) |
| 4 | 设墙预览合法绿/非法红+路线+步数 | Legal/Illegal/不mutate ✅;步数仅 `>=0` 弱断言 | PreviewLayerView 颜色 | (a) 补步数精确;(b) 场景2;(c) 手动颜色 |
| 5 | 墙耗尽禁拾 | 无(纯 UI 输入) | `BoardView.Render` `wallable=WallsLeft>0 && !IsFinished` ✅ | (a) 文档化预览忽略墙数;(c) 手动 |
| 6 | 终局胜者+循环 | PlayerWon 事件 ✅ | ShowWinner+home ✅ | (c) 手动 |
| 7 | mouse_exit 清除预览 | 无(纯 UI) | `MouseExited→SlotCleared→preview.Clear` ✅ | (c) 手动 |

## 4. (a) 逻辑层测试补强

**文件**:`tests/Quoridor.Application.Tests/PreviewServiceTests.cs`(末尾追加,纯加测试)

| 测试 | 断言 |
|------|------|
| `Legal_wall_on_Kid_returns_routes` | Kid 初始局面(`GameSetup.CreateKid2P`),合法墙 → `Legal==true`、`Routes.Length==2`(P1+P2)、两步数 `>=0` |
| `Wall_blocking_paths_on_Kid_is_illegal` | Kid 变体下构造封死 P1 路径的墙 → `Legal==false`、`Reason==WallBlocksAllPaths` |
| `Step_count_is_exact_for_known_position` | 构造已知中盘局面(一面墙拉长某方路线),断言该方**精确步数**(非 `>=0`),锁定 PathFinder 正确性。具体局面与期望值在实现时由 PathFinder 实跑确定并写死 |
| `Preview_ignores_wall_budget` | 墙耗尽 state(`WallsLeft==0`)调 `PoseWall` → 仍返回 `Legal==true`(文档化:预览只管结构合法性,不管墙数;墙数禁拾由 `BoardView.Render` 另管) |

**不做**:不补 SeatsBuilder/SeatMap(已覆盖四组合)、不补 GameSession(41 测试)。

## 5. (b) 半自动回归

**文件**:`demo/Quoridor.Demo/Program.cs` 加 `--acceptance` 子命令(沿用 `--gen-replays` 模式),约 80 行。

### 5.1 场景 1 · 人机 Kid P2先手换位

分两步(HumanPlayer 在 headless 不会自动决策,无法跑到终局;故换位逻辑与终局流分开验):

**1a · 换位断言**(验 SeatsBuilder + SeatMap,即验收项 2 的"验证换位"):
```
cfg = new GameConfig(Kid, VsAi, Medium, P2)
seats = SeatsBuilder.Build(cfg)
断言 seats[0].Id==P1 && !seats[0].IsHuman   // AI 先手(P1 座位=AI)
断言 seats[1].Id==P2 &&  seats[1].IsHuman   // 人类后手
map = SeatMap.ForFirstMove(P2)
断言 map.ToDisplayNumber(P1)==2 && map.ToDisplayNumber(P2)==1   // P1 显作玩家2, P2 显作玩家1
```

**1b · Kid 终局流**(验 Kid 变体下一局能跑到底,即验收项 6 的逻辑流):
```
seats = [AiPlayerFactory.Create(P1, Easy), AiPlayerFactory.Create(P2, Easy)]  // 两 AI 驱动到终局
session = new GameSession(BoardConfig.Kid, seats)
订阅 EventOccurred, dump 每手 (Ply/Pwho/类型/坐标/剩余墙)
session.Start()
断言 session.State.IsFinished && Winner != null
dump 胜者 + 总手数 + 记谱
```

> "人机"的真实视觉体验(人类实操 P2 走子)属验收项 2/3 的手动部分,交 (c)。

### 5.2 场景 2 · 设墙预览合法/非法

```
state = GameSetup.CreateKid2P()                              // Kid 初始局面
legal   = PreviewService.PoseWall(state, <不切断路径的合法墙>)  // 镜像现有 Standard 合法用例
// 构造 P1 被封死的 state(参照现有 Wall_blocking_all_paths_is_illegal 的 Kid 版构造)
illegal = PreviewService.PoseWall(<封死 state>, <封死墙>)
断言 legal.Legal && legal.Routes.Length==2
断言 !illegal.Legal && illegal.Reason==WallBlocksAllPaths
dump 两结果 (Legal/Reason/各棋子步数)
```

### 5.3 输出与退出码

- 每场景末尾打印 `[PASS]` / `[FAIL] <原因>`
- 任一 FAIL → 退出码 1;全 PASS → 退出码 0
- 输出人类可读,便于作为验收 artifact 重定向到文件

### 5.4 不在 (b) 做

- hot-seat 两人交替:逻辑层 HumanPlayer 等输入,无"交替"概念;交给 (c) 手动
- Godot 项目加载检查:用现成命令 `godot-mono --headless --path src/Quoridor.UI --quit`(不写代码,实现时由我跑一次贴日志)

## 6. (c) 手动验收清单

**文件**:`docs/superpowers/acceptance/2026-07-06-plan4-acceptance-checklist.md`

7 项,每项三段:**操作步骤** / **期望视觉** / **期望日志信号**(`GD.Print`/`PushWarning` 关键字)。条目对应 §3 表的 #2-#7(#1 已过,列入背景)。

清单内容在实现阶段填写具体步骤;spec 只定结构:每项必须含可对照的视觉期望 + 至少一条日志关键字,使"看日志"能部分替代"凭眼睛"。

## 7. 验证标准(合入门槛)

1. `dotnet test Quoridor.slnx` → Domain 74 + Application 41(+4) + UI.Logic 45 = 164 全绿
2. `dotnet run --project demo/Quoridor.Demo -- --acceptance` → 两场景全 PASS,退出码 0
3. `godot-mono --headless --path src/Quoridor.UI --quit` → 加载无报错(运行门槛,同 spec §9.3)
4. 手动清单 7 项由用户勾过(视觉部分,CLI 无法替代)

## 8. 风险与回退

- **低风险**:不加生产代码,只加测试 + demo 子命令 + 文档。最坏情况:某条测试因 PathFinder 实际步数与写死值不符而红 → 实现时以实跑值为准写死,不改生产代码。
- **回退**:三层交付互相独立,(a)/(b)/(c) 任一可单独保留或舍弃。
