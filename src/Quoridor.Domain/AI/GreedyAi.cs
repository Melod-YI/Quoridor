using System;
using System.Collections.Generic;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;

namespace Quoridor.Domain.AI;

public sealed class GreedyAi : IQuoridorAi
{
    private const int VisitedPenalty = 100;  // 压过评估等分(0), 不压过 WinScore(100000) 或真前进(+10)

    // 跨调用历史位置(按玩家分键)。根节点惩罚走回站过的格子 → 消除对称 2-格环。
    private readonly Dictionary<PlayerId, HashSet<Cell>> _visited = new();
    private readonly object _lock = new();

    public IGameCommand Choose(GameState state, Difficulty difficulty)
    {
        var actions = AiActionSet.Generate(state);
        if (actions.Length == 0)
            throw new InvalidOperationException("无合法动作（Quoridor 中玩家总有合法走子，不应发生）");

        var me = state.ActivePlayer;
        var visited = RecordAndGetSnapshot(state, me);

        IGameCommand best = actions[0];
        int bestScore = int.MinValue;
        foreach (var a in actions)
        {
            var r = RuleEngine.ValidateAndApply(state, a);
            if (r.State is null) continue; // AiActionSet 已过滤，理论不触发
            int score = Evaluator.Evaluate(r.State, me);
            // A: 走回自己站过的格子 → 减分(消除跨回合 2-格环)。
            if (a is MovePawnCommand mc && visited.Contains(mc.To))
                score -= VisitedPenalty;
            if (score > bestScore)
            {
                bestScore = score;
                best = a;
            }
        }
        return best;
    }

    /// <summary>记录当前己方棋子位置到历史, 返回不可变快照供只读。</summary>
    private HashSet<Cell> RecordAndGetSnapshot(GameState state, PlayerId me)
    {
        var myPos = state.PawnOf(me).Pos;
        lock (_lock)
        {
            if (!_visited.TryGetValue(me, out var set))
            {
                set = new HashSet<Cell>();
                _visited[me] = set;
            }
            set.Add(myPos);
            return new HashSet<Cell>(set);
        }
    }
}
