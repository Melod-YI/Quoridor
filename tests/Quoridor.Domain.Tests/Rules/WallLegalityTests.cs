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
    public void Overlapping_same_slot_rejected_but_crossing_orientation_legal()
    {
        var s = GameSetup.CreateStandard2P() with
        {
            Walls = ImmutableArray.Create(
                new WallPos(new Cell(4, 3), WallOrient.Horizontal)),
        };
        // 同位重复（同 anchor 同朝向）：共享两条边 → 重叠
        Assert.Equal(RejectReason.WallOverlap,
            WallLegality.Validate(s, new WallPos(new Cell(4, 3), WallOrient.Horizontal)));
        // 不同朝向同 anchor：H 阻竖直通道、V 阻水平通道，无共享边，仅交于一点 → 合法（墙可交叉）
        Assert.Null(WallLegality.Validate(s, new WallPos(new Cell(4, 3), WallOrient.Vertical)));
    }

    [Fact]
    public void Wall_that_seals_player_rejected()
    {
        // P1 置于左下角 (0,0)；已放 V(0,0) 阻断东出口（北出口仍开，P1 可达 row8，故该墙合法）。
        var initial = GameSetup.CreateStandard2P();
        var p1 = initial.PawnOf(PlayerId.P1);
        var sealedState = initial with
        {
            Pawns = initial.Pawns.Replace(p1, p1 with { Pos = new Cell(0, 0) }),
            Walls = ImmutableArray.Create(
                new WallPos(new Cell(0, 0), WallOrient.Vertical)),
        };
        // 再放 H(0,0) 封死北出口：P1 东(V)北(H)皆堵，南西为棋盘边界 → 无路可达 row8
        // → WallBlocksAllPaths（H 与 V 无共享边，不触发重叠，直接到可达性校验失败）
        var reason = WallLegality.Validate(sealedState, new WallPos(new Cell(0, 0), WallOrient.Horizontal));
        Assert.Equal(RejectReason.WallBlocksAllPaths, reason);
    }
}
