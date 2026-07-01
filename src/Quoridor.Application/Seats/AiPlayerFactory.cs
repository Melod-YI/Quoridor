using Quoridor.Domain.AI;
using Quoridor.Domain.Core;

namespace Quoridor.Application.Seats;

/// <summary>难度→AI 实现映射(债9): Easy=GreedyAi, Medium/Hard=MinimaxAi(深度由 Difficulty 决定)。</summary>
public static class AiPlayerFactory
{
    public static AiPlayer Create(PlayerId id, Difficulty difficulty)
    {
        IQuoridorAi impl = difficulty == Difficulty.Easy
            ? new GreedyAi()
            : new MinimaxAi();
        return new AiPlayer(id, impl, difficulty);
    }
}
