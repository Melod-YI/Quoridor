using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Domain.Tests.Rules;

public class MoveLegalityTests
{
    [Fact]
    public void Empty_board_p1_can_step_to_four_neighbors()
    {
        var s = GameSetup.CreateStandard2P();
        var targets = MoveLegality.LegalTargets(s);
        Assert.Contains(new Cell(4, 1), targets);
        Assert.Contains(new Cell(3, 0), targets);
        Assert.Contains(new Cell(5, 0), targets);
        // row 0 在底边，不能向南出界
        Assert.DoesNotContain(new Cell(4, -1), targets);
    }

    [Fact]
    public void Straight_jump_over_adjacent_opponent()
    {
        // P1 在 (4,0)，把 P2 放到 (4,1) 相邻，无墙 → 可直跳到 (4,2)
        var s = PlaceP2At(GameSetup.CreateStandard2P(), new Cell(4, 1));
        var targets = MoveLegality.LegalTargets(s);
        Assert.Contains(new Cell(4, 2), targets);
    }

    [Fact]
    public void Jump_blocked_by_wall_behind_opponent_allows_diagonal()
    {
        // P1(4,0) 对 P2(4,1)；身后(4,1)-(4,2) 有水平墙 → 直跳被挡，允许斜跳到 (3,1)/(5,1)
        var s = PlaceP2At(GameSetup.CreateStandard2P(), new Cell(4, 1));
        s = s with
        {
            Walls = System.Collections.Immutable.ImmutableArray.Create(
                new WallPos(new Cell(4, 1), WallOrient.Horizontal)),
        };
        var targets = MoveLegality.LegalTargets(s);
        Assert.DoesNotContain(new Cell(4, 2), targets); // 直跳被封
        Assert.Contains(new Cell(3, 1), targets);
        Assert.Contains(new Cell(5, 1), targets);
    }

    [Fact]
    public void Step_blocked_by_wall_is_excluded()
    {
        var s = GameSetup.CreateStandard2P() with
        {
            Walls = System.Collections.Immutable.ImmutableArray.Create(
                new WallPos(new Cell(4, 0), WallOrient.Horizontal)),
        };
        var targets = MoveLegality.LegalTargets(s);
        Assert.DoesNotContain(new Cell(4, 1), targets);
    }

    private static GameState PlaceP2At(GameState s, Cell c) =>
        s with { Pawns = s.Pawns.Replace(s.PawnOf(PlayerId.P2), s.PawnOf(PlayerId.P2) with { Pos = c }) };
}
