using System.Collections.Immutable;
using System.Linq;
using Quoridor.Domain.Core;
using Quoridor.Domain.Path;
using Xunit;

namespace Quoridor.Domain.Tests.Path;

public class ReachabilityTests
{
    [Fact]
    public void Empty_board_all_players_reach_goal()
    {
        Assert.True(Reachability.AllPlayersCanReachGoal(GameSetup.CreateStandard2P()));
        Assert.True(Reachability.AllPlayersCanReachGoal(GameSetup.CreateStandard4P()));
    }

    [Fact]
    public void Legal_partial_wall_keeps_reachability()
    {
        var s = GameSetup.CreateStandard2P() with
        {
            Walls = System.Collections.Immutable.ImmutableArray.Create(
                new WallPos(new Cell(4, 3), WallOrient.Horizontal)),
        };
        Assert.True(Reachability.AllPlayersCanReachGoal(s));
    }

    [Fact]
    public void Wall_sealing_p1_off_returns_false()
    {
        // 在 P1 起点行(row0)上方沿整行水平墙封死，堵死 P1 北上
        var walls = new System.Collections.Generic.List<WallPos>();
        for (int c = 0; c < 8; c++)
            walls.Add(new WallPos(new Cell(c, 0), WallOrient.Horizontal));
        var s = GameSetup.CreateStandard2P() with
        {
            Walls = walls.ToImmutableArray(),
        };
        Assert.False(Reachability.AllPlayersCanReachGoal(s));
    }
}
