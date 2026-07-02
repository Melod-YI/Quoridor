# Quoridor UI MVP 手动验收清单

启动: `godot-mono --path src/Quoridor.UI`(在 worktree `plan4-quoridor-ui` 内)

## 验收项(spec §9.2)

- [x] 1. 人机标准 9x9, 玩家1 先手, 走完一局至胜(终局显示胜者 + 回到开始页) — 2026-07-03 通过
- [ ] 2. 人机 Kid 7x7, 玩家2 先手(AI 先走, 验证座位换位)
- [ ] 3. hot-seat 两人交替走子与设墙
- [ ] 4. 设墙悬浮预览: 合法(绿)+路线+步数; 非法(红, 如切断玩家路径)
- [ ] 5. 墙数耗尽后墙槽不可拾取(input_ray_pickable=false)
- [ ] 6. 终局 → 回到开始页 → 可再开一局(循环)
- [ ] 7. mouse_exit 槽后预览清除

## 已知运行时顾虑(Phase C 只 build 不跑, 需在此验收确认)

来自 Task 12 BoardView:
- MouseEntered/MouseExited 信号需 Area3D 碰撞层 + viewport 相机/光照才触发;3D 拾取依赖 InputRayPickable(已设 true)。**验收项 4/7 依赖此**——若悬浮预览不出现,先查这里。
- SyncWalls 同步调用,前提 Render 在主线程事件回调调用(GameViewRoot.OnEvent 路径)。若在物理回调里调 Render 会出问题(当前不会)。

来自 Task 13 PreviewLayerView:
- ImmediateMesh 在 MeshInstance3D 上设 MaterialOverride 可能不生效 → 路线/候选墙可能不显示或无透明度。**若验收项 4 看不到预览**,改为 `_routeMesh.SurfaceSetMaterial(index, _lineMat)`。
- 多 route 共用一个 LineStrip surface → route 间可能有多余连线。**若路线有多余线段**,改为每 route 独立 SurfaceBegin/End 或用 Lines。
- 路线顶点用 CellToWorld(格角)而候选墙用 +0.5(中心)→ 路线可能半格偏移。**若路线偏离格子**,route 顶点也加 0.5 偏移。

全局:
- BoardView 棋子/格子用 CellToWorld(格角 origin),墙用中心 → 棋子可能落在格角而非格中心。**若棋子位置偏**,统一坐标语义。

## 操作步骤(验收项 1)

1. 启动 → StartFrame 页(4 个下拉 + 开始按钮;初始无默认选中,属轻微 UX 缺陷,不影响功能)。
2. 选 标准9x9 / 人机 / 中等 / 玩家1先 → 开始对局。
3. GameView:点击格子走子;鼠标悬停墙槽看预览(绿合法+路线步数);点击墙槽设墙。
4. 往复至某方抵达对边 → 终局显示"玩家X 获胜!"+ "回到开始页"按钮。
5. 点回到开始页 → 回 StartFrame → 可再开。

## 已知坑/回归记录(验收时填写)

### 验收项 1 通过(2026-07-03)

人机标准9x9 / 玩家1先 / Easy。日志证据:
- P1 走 (4,0)→(4,1)→(4,2)→(4,3)→设墙 H(4,0)→(4,5)→(4,6)→(4,7)→(4,8) 抵达目标行
- AI(P2) 正常回应每步;`对局终局 winner=P1`;点"回到开始页"触发 `MainController.EndSession`
- 无崩溃/异常, exit 0

### 验收过程中发现并修复的视觉 bug(见 git log)

- 棋盘偏下半截 + 无可见网格 + 点击全 IllegalMove(根因 CellToWorld 返回格角非中心 + 相机未瞄准 + 底排出框)
- 墙/槽落在格中央非格交点(3 处重复公式错, 集中为 BoardLayout.WallCenter)
- 墙与槽颜色过近、网格 NoDepthTest 盖住棋子、HUD 标签阻挡点击、正交 55° 太平
- 修复后:格中心坐标、可见粗网格槽、墙对齐槽、墙暖橙棕、透视 60° 俯角、HUD 鼠标穿透

### 未验项(2-7)

合并后单独验(不阻塞 MVP 基线): Kid/玩家2先手/hot-seat/预览显示/墙数耗尽禁拾/循环/mouse_exit 清除。
