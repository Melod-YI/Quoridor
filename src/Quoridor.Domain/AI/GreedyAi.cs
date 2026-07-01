using System;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;

namespace Quoridor.Domain.AI;

public sealed class GreedyAi : IQuoridorAi
{
    public IGameCommand Choose(GameState state, Difficulty difficulty)
    {
        var actions = AiActionSet.Generate(state);
        if (actions.Length == 0)
            throw new InvalidOperationException("无合法动作（Quoridor 中玩家总有合法走子，不应发生）");

        var me = state.ActivePlayer;
        IGameCommand best = actions[0];
        int bestScore = int.MinValue;
        foreach (var a in actions)
        {
            var r = RuleEngine.ValidateAndApply(state, a);
            if (r.State is null) continue; // AiActionSet 已过滤，理论不触发
            int score = Evaluator.Evaluate(r.State, me);
            if (score > bestScore)
            {
                bestScore = score;
                best = a;
            }
        }
        return best;
    }
}
