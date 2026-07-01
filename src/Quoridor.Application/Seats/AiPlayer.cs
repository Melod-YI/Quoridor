using Quoridor.Domain.AI;
using Quoridor.Domain.Core;

namespace Quoridor.Application.Seats;

public sealed class AiPlayer : IPlayer
{
    private readonly IQuoridorAi _ai;
    private readonly Difficulty _difficulty;

    public PlayerId Id { get; }
    public bool IsHuman => false;

    public AiPlayer(PlayerId id, IQuoridorAi ai, Difficulty difficulty)
    {
        Id = id;
        _ai = ai;
        _difficulty = difficulty;
    }

    public IGameCommand? ProposeMove(GameState state)
    {
        // 债7: 入口守卫, 对已结束局面不调 AI(避免 RuleEngine 以 GameFinished 拒绝)
        if (state.IsFinished) return null;
        return _ai.Choose(state, _difficulty);
    }
}
