using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;

namespace Quoridor.Domain.AI;

public sealed class MinimaxAi : IQuoridorAi
{
    private const int VisitedPenalty = 100;  // 压过评估等分(0), 不压过 WinScore(100000) 或真前进(+10)

    // 跨调用历史位置(按玩家分键, 防自对弈共用实例串扰)。根节点惩罚走回站过的格子 → 消除对称 2-格环。
    private readonly Dictionary<PlayerId, HashSet<Cell>> _visited = new();
    private readonly object _lock = new();

    public IGameCommand Choose(GameState state, Difficulty difficulty)
    {
        int depth = difficulty switch
        {
            Difficulty.Easy => 1,
            Difficulty.Medium => 2,
            Difficulty.Hard => 3,
            _ => 1,
        };

        var me = state.ActivePlayer;
        var visited = RecordAndGetSnapshot(state, me);

        var actions = Order(state, AiActionSet.Generate(state), me, descending: true);
        if (actions.Length == 0)
            throw new InvalidOperationException("无合法动作");

        // 根节点并行: 各子树独立 AlphaBeta(只读不可变 state + 纯函数, 线程安全)。
        // 共享 bestScore 作 alpha(Volatile 原子读)恢复根级剪枝——避免每子树用 MinValue 暴力全搜。
        // 注: 并行下 alpha 更新有延迟(剪枝不如单线程及时), 但远胜无剪枝。
        IGameCommand best = actions[0];
        int bestScore = int.MinValue;
        var lockObj = new object();
        Parallel.For(0, actions.Length, i =>
        {
            var r = RuleEngine.ValidateAndApply(state, actions[i]);
            if (r.State is null) return;
            int alpha = Volatile.Read(ref bestScore);
            int score = AlphaBeta(r.State, depth - 1, alpha, int.MaxValue, me);
            // A: 走回自己站过的格子 → 减分(消除跨回合 2-格环)。
            if (actions[i] is MovePawnCommand mc && visited.Contains(mc.To))
                score -= VisitedPenalty;
            lock (lockObj)
            {
                if (score > bestScore) { bestScore = score; best = actions[i]; }
            }
        });
        return best;
    }

    /// <summary>记录当前己方棋子位置到历史, 返回不可变快照供并行只读。</summary>
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

    private static int AlphaBeta(GameState state, int depth, int alpha, int beta, PlayerId me)
    {
        if (depth <= 0 || state.IsFinished)
            return Evaluator.Evaluate(state, me);

        bool maximizing = state.ActivePlayer == me;
        var actions = Order(state, AiActionSet.Generate(state), me, descending: maximizing);
        int value = maximizing ? int.MinValue : int.MaxValue;

        foreach (var a in actions)
        {
            var r = RuleEngine.ValidateAndApply(state, a);
            if (r.State is null) continue;
            int child = AlphaBeta(r.State, depth - 1, alpha, beta, me);

            if (maximizing)
            {
                if (child > value) value = child;
                if (value > alpha) alpha = value;
                if (alpha >= beta) break;
            }
            else
            {
                if (child < value) value = child;
                if (value < beta) beta = value;
                if (alpha >= beta) break;
            }
        }
        return value;
    }

    /// <summary>1-ply 评估排序：max 节点降序、min 节点升序，提升剪枝。</summary>
    private static ImmutableArray<IGameCommand> Order(
        GameState state, ImmutableArray<IGameCommand> actions, PlayerId me, bool descending)
    {
        var scored = new List<(IGameCommand Cmd, int Score)>(actions.Length);
        foreach (var a in actions)
        {
            var r = RuleEngine.ValidateAndApply(state, a);
            int s = r.State is null ? int.MinValue : Evaluator.Evaluate(r.State, me);
            scored.Add((a, s));
        }
        var ordered = descending
            ? scored.OrderByDescending(t => t.Score)
            : scored.OrderBy(t => t.Score);
        return ordered.Select(t => t.Cmd).ToImmutableArray();
    }
}
