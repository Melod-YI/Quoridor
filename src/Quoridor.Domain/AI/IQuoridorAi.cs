using Quoridor.Domain.Core;

namespace Quoridor.Domain.AI;

public interface IQuoridorAi
{
    IGameCommand Choose(GameState state, Difficulty difficulty);
}
