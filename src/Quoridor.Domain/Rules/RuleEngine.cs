using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Quoridor.Domain.Core;
using Quoridor.Domain.Path;

namespace Quoridor.Domain.Rules;

public static class RuleEngine
{
    public readonly record struct ApplyResult(GameState? State, ImmutableArray<IGameEvent> Events);

    public static ApplyResult ValidateAndApply(GameState state, IGameCommand command)
    {
        if (state.IsFinished)
        {
            return command switch
            {
                MovePawnCommand m => RejectMove(state.ActivePlayer, m.To, RejectReason.GameFinished),
                PlaceWallCommand w => RejectWall(state.ActivePlayer, w.Wall, RejectReason.GameFinished),
                SurrenderCommand => new ApplyResult(null, ImmutableArray<IGameEvent>.Empty),  // 终局投降无意义, 静默
                _ => throw new ArgumentOutOfRangeException(nameof(command)),
            };
        }
        return command switch
        {
            MovePawnCommand m => ApplyMove(state, state.ActivePlayer, m),
            PlaceWallCommand w => ApplyWall(state, state.ActivePlayer, w),
            SurrenderCommand => ApplySurrender(state, state.ActivePlayer),
            _ => throw new ArgumentOutOfRangeException(nameof(command)),
        };
    }

    private static ApplyResult ApplyMove(GameState state, PlayerId who, MovePawnCommand cmd)
    {
        var targets = MoveLegality.LegalTargets(state);
        if (!targets.Contains(cmd.To))
            return RejectMove(who, cmd.To, RejectReason.IllegalMove);

        var pawn = state.PawnOf(who);
        var kind = Classify(pawn.Pos, cmd.To);
        var newPawns = state.Pawns.Replace(pawn, pawn with { Pos = cmd.To });
        var events = new List<IGameEvent> { new PawnMoved(who, pawn.Pos, cmd.To, kind) };

        GameState next = state with { Pawns = newPawns };
        if (GoalChecker.Reached(state.PlayerOf(who).Goal, cmd.To, state.Config))
        {
            next = next with { Phase = Phase.Finished, Winner = who };
            events.Add(new PlayerWon(who));
        }
        else
        {
            var nx = NextPlayer(state);
            next = next with { ActivePlayer = nx };
            events.Add(new TurnPassed(nx));
        }
        return new ApplyResult(next, events.ToImmutableArray());
    }

    private static ApplyResult ApplyWall(GameState state, PlayerId who, PlaceWallCommand cmd)
    {
        var player = state.PlayerOf(who);
        if (player.WallsLeft <= 0)
            return RejectWall(who, cmd.Wall, RejectReason.NoWallsLeft);

        var reason = WallLegality.Validate(state, cmd.Wall);
        if (reason is not null)
            return RejectWall(who, cmd.Wall, reason.Value);

        var newPlayers = state.Players.Replace(player, player with { WallsLeft = player.WallsLeft - 1 });
        var nx = NextPlayer(state);
        var next = state with { Players = newPlayers, Walls = state.Walls.Add(cmd.Wall), ActivePlayer = nx };
        var events = new List<IGameEvent> { new WallPlaced(who, cmd.Wall), new TurnPassed(nx) };
        return new ApplyResult(next, events.ToImmutableArray());
    }

    private static PlayerId NextPlayer(GameState state)
    {
        int n = state.Players.Length;
        return (PlayerId)(((int)state.ActivePlayer + 1) % n);
    }

    /// <summary>投降: 当前活跃玩家认输, 对方(2 人 MVP 下的下一玩家)获胜, 终局。</summary>
    private static ApplyResult ApplySurrender(GameState state, PlayerId who)
    {
        var winner = NextPlayer(state);
        var next = state with { Phase = Phase.Finished, Winner = winner };
        var events = ImmutableArray.Create<IGameEvent>(new PlayerSurrendered(who), new PlayerWon(winner));
        return new ApplyResult(next, events);
    }

    private static MoveKind Classify(Cell from, Cell to)
    {
        int dc = Math.Abs(to.Col - from.Col);
        int dr = Math.Abs(to.Row - from.Row);
        if (dc + dr == 1) return MoveKind.Step;
        if ((dc == 2 && dr == 0) || (dr == 2 && dc == 0)) return MoveKind.Jump;
        return MoveKind.DiagonalJump;
    }

    private static ApplyResult RejectMove(PlayerId who, Cell to, RejectReason r) =>
        new(null, ImmutableArray.Create<IGameEvent>(new MoveRejected(who, to, r)));

    private static ApplyResult RejectWall(PlayerId who, WallPos w, RejectReason r) =>
        new(null, ImmutableArray.Create<IGameEvent>(new WallRejected(who, w, r)));
}
