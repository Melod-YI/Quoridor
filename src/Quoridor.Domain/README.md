# Quoridor.Domain

Quoridor 游戏 domain 核心类库（零 Godot 依赖）。

## 构建

```
dotnet build
```

## 测试

```
dotnet test
```

## 职责

- `Core`：不可变数据模型、命令、事件、初始局面工厂。
- `Path`：无向图 `BoardGraph`、BFS `PathFinder`、`Reachability`。
- `Rules`：`MoveLegality` / `WallLegality` / `RuleEngine.ValidateAndApply`。
- `Notation`：Modern Algebraic Notation 编/解码与回放。

## 唯一状态变更入口

```
ApplyResult r = RuleEngine.ValidateAndApply(state, command);
// r.State 非空 -> 已应用；为空 -> 被拒绝，state 不变，r.Events 含 Rejected。
```

后续 AI、Application、UI 在其上叠加，不修改本库内部状态结构。
