using System.Collections.Generic;
using System.Collections.Immutable;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;

namespace Quoridor.Domain.AI;

public static class AiActionSet
{
    public static ImmutableArray<IGameCommand> Generate(GameState state)
    {
        var actions = new List<IGameCommand>();

        // 走子：复用 MoveLegality 的合法目标格
        foreach (var to in MoveLegality.LegalTargets(state))
            actions.Add(new MovePawnCommand(to));

        // 设墙：枚举所有 anchor×朝向，仅保留 WallLegality 判定合法的
        var player = state.PlayerOf(state.ActivePlayer);
        if (player.WallsLeft > 0)
        {
            var cfg = state.Config;
            for (int c = 0; c < cfg.MaxIndex; c++)
                for (int r = 0; r < cfg.MaxIndex; r++)
                {
                    var anchor = new Cell(c, r);
                    var h = new WallPos(anchor, WallOrient.Horizontal);
                    if (WallLegality.Validate(state, h) is null)
                        actions.Add(new PlaceWallCommand(h));
                    var v = new WallPos(anchor, WallOrient.Vertical);
                    if (WallLegality.Validate(state, v) is null)
                        actions.Add(new PlaceWallCommand(v));
                }
        }
        return actions.ToImmutableArray();
    }
}
