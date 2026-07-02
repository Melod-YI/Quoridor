using System.Collections.Immutable;
using Quoridor.Domain.Core;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Domain.Tests.Rules;

public class WallLegalityTests
{
    [Fact]
    public void In_bounds_legal_wall_returns_null()
    {
        var s = GameSetup.CreateStandard2P();
        var w = new WallPos(new Cell(0, 0), WallOrient.Horizontal);
        Assert.Null(WallLegality.Validate(s, w));
    }

    [Fact]
    public void Out_of_bounds_anchor_rejected()
    {
        var s = GameSetup.CreateStandard2P();
        // MaxIndex=8，合法 anchor 上限 7
        Assert.Equal(RejectReason.WallOutOfBounds,
            WallLegality.Validate(s, new WallPos(new Cell(8, 0), WallOrient.Horizontal)));
        Assert.Equal(RejectReason.WallOutOfBounds,
            WallLegality.Validate(s, new WallPos(new Cell(0, 8), WallOrient.Vertical)));
    }

    [Fact]
    public void Overlapping_same_slot_rejected()
    {
        var s = GameSetup.CreateStandard2P() with
        {
            Walls = ImmutableArray.Create(
                new WallPos(new Cell(4, 3), WallOrient.Horizontal)),
        };
        // 同 anchor 同朝向: 共享两条边 → 重叠
        Assert.Equal(RejectReason.WallOverlap,
            WallLegality.Validate(s, new WallPos(new Cell(4, 3), WallOrient.Horizontal)));
    }

    [Fact]
    public void Plus_intersection_same_anchor_different_orientation_rejected()
    {
        // 不同朝向同 anchor: H 与 V 都穿过同一格角, 形成 "+" 字交叉 → 非法
        // (T 字结构则合法, 见 T_junction_offset_anchor_legal)
        var s = GameSetup.CreateStandard2P() with
        {
            Walls = ImmutableArray.Create(
                new WallPos(new Cell(4, 3), WallOrient.Horizontal)),
        };
        Assert.Equal(RejectReason.WallPlusIntersection,
            WallLegality.Validate(s, new WallPos(new Cell(4, 3), WallOrient.Vertical)));
    }

    [Fact]
    public void T_junction_offset_anchor_legal()
    {
        // H(4,3) 与 V(4,4): V 的下端落于 H 穿过的格角, 形成 T 字(非 +) → 合法
        var s = GameSetup.CreateStandard2P() with
        {
            Walls = ImmutableArray.Create(
                new WallPos(new Cell(4, 3), WallOrient.Horizontal)),
        };
        Assert.Null(WallLegality.Validate(s, new WallPos(new Cell(4, 4), WallOrient.Vertical)));
    }

    [Fact]
    public void Wall_that_seals_player_rejected()
    {
        // P1 置于 (0,0); 已放 V(0,0) 阻东出口。再放 H(0,1) 阻北出口, 把 P1 封死在 (0,0)-(0,1) 两格。
        // V(0,0) 与 H(0,1) 不同 anchor, 在格角 (1,2) 成 T 字(非 +), 通过结构校验;
        // 但 P1 无路可达北目标 row8 → WallBlocksAllPaths。
        var initial = GameSetup.CreateStandard2P();
        var p1 = initial.PawnOf(PlayerId.P1);
        var sealedState = initial with
        {
            Pawns = initial.Pawns.Replace(p1, p1 with { Pos = new Cell(0, 0) }),
            Walls = ImmutableArray.Create(
                new WallPos(new Cell(0, 0), WallOrient.Vertical)),
        };
        var reason = WallLegality.Validate(sealedState, new WallPos(new Cell(0, 1), WallOrient.Horizontal));
        Assert.Equal(RejectReason.WallBlocksAllPaths, reason);
    }
}
