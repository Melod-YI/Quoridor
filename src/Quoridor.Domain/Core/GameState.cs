using System.Collections.Immutable;
using System.Linq;

namespace Quoridor.Domain.Core;

public sealed record GameState(
    BoardConfig Config,
    ImmutableArray<PlayerState> Players,
    ImmutableArray<Pawn> Pawns,
    ImmutableArray<WallPos> Walls,
    PlayerId ActivePlayer,
    Phase Phase,
    PlayerId? Winner)
{
    public Pawn PawnOf(PlayerId id) => Pawns.Single(p => p.Owner == id);
    public PlayerState PlayerOf(PlayerId id) => Players.Single(p => p.Id == id);
    public bool IsFinished => Phase == Phase.Finished;
}
