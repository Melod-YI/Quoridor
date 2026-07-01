using Quoridor.Domain.Core;

namespace Quoridor.Application.Seats;

public sealed class HumanPlayer : IPlayer
{
    public PlayerId Id { get; }
    public bool IsHuman => true;

    public HumanPlayer(PlayerId id) => Id = id;

    public IGameCommand? ProposeMove(GameState state) => null;
}
