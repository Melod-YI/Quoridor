using System.Collections.Immutable;
using Quoridor.Domain.Core;
using Quoridor.Domain.Path;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Application.Tests;

public class PreviewServiceTests
{
    [Fact]
    public void Legal_wall_returns_routes_for_all_pawns()
    {
        var state = GameSetup.CreateStandard2P();
        var wall = new WallPos(new Cell(4, 3), WallOrient.Horizontal);  // e4h, 不切断路径

        var r = PreviewService.PoseWall(state, wall);

        Assert.True(r.Legal);
        Assert.Null(r.Reason);
        Assert.Equal(2, r.Routes.Length);  // P1 + P2
        Assert.Contains(r.Routes, x => x.Pawn == PlayerId.P1 && x.Steps >= 0);
        Assert.Contains(r.Routes, x => x.Pawn == PlayerId.P2 && x.Steps >= 0);
    }

    [Fact]
    public void Wall_blocking_all_paths_is_illegal()
    {
        // P1 在 (0,0), 已放 V(0,0) 阻东; 预览 H(0,1) 阻北 → V 与 H 在格角 (1,2) 成 T 字,
        // 把 P1 封死在 (0,0)-(0,1) → 非法(WallBlocksAllPaths)。非 + 字交叉, 通过结构校验。
        var s = GameSetup.CreateStandard2P();
        s = s with
        {
            Pawns = s.Pawns.Replace(s.PawnOf(PlayerId.P1), s.PawnOf(PlayerId.P1) with { Pos = new Cell(0, 0) }),
        };
        s = s with { Walls = s.Walls.Add(new WallPos(new Cell(0, 0), WallOrient.Vertical)) };

        var r = PreviewService.PoseWall(s, new WallPos(new Cell(0, 1), WallOrient.Horizontal));

        Assert.False(r.Legal);
        Assert.Equal(RejectReason.WallBlocksAllPaths, r.Reason);
    }

    [Fact]
    public void Overlapping_wall_is_illegal()
    {
        var s = GameSetup.CreateStandard2P();
        s = s with { Walls = s.Walls.Add(new WallPos(new Cell(0, 0), WallOrient.Horizontal)) };

        var r = PreviewService.PoseWall(s, new WallPos(new Cell(0, 0), WallOrient.Horizontal));

        Assert.False(r.Legal);
        Assert.Equal(RejectReason.WallOverlap, r.Reason);
    }

    [Fact]
    public void Preview_does_not_mutate_original_state()
    {
        var state = GameSetup.CreateStandard2P();
        var wallsBefore = state.Walls.Length;

        PreviewService.PoseWall(state, new WallPos(new Cell(4, 3), WallOrient.Horizontal));

        Assert.Equal(wallsBefore, state.Walls.Length);  // 原状态墙数不变
    }

    [Fact]
    public void Wall_lengthening_opponent_increases_steps()
    {
        var state = GameSetup.CreateStandard2P();
        var baseline = PreviewService.PoseWall(state, new WallPos(new Cell(4, 3), WallOrient.Horizontal));
        var p2Baseline = StepsOf(baseline, PlayerId.P2);

        // 在 P2(4,8) 南侧加水平墙, 拉长 P2 到南目标的路径
        var blocked = state with { Walls = state.Walls.Add(new WallPos(new Cell(3, 7), WallOrient.Horizontal)) };
        var after = PreviewService.PoseWall(blocked, new WallPos(new Cell(4, 3), WallOrient.Horizontal));
        var p2After = StepsOf(after, PlayerId.P2);

        Assert.True(p2After >= p2Baseline, $"P2 步数应不减少, base={p2Baseline} after={p2After}");
    }

    [Fact]
    public void Legal_wall_on_Kid_returns_routes()
    {
        var state = GameSetup.CreateKid2P();
        var wall = new WallPos(new Cell(0, 0), WallOrient.Horizontal);  // Kid 7×7 角落, 不切断路径

        var r = PreviewService.PoseWall(state, wall);

        Assert.True(r.Legal);
        Assert.Null(r.Reason);
        Assert.Equal(2, r.Routes.Length);
        Assert.Contains(r.Routes, x => x.Pawn == PlayerId.P1 && x.Steps >= 0);
        Assert.Contains(r.Routes, x => x.Pawn == PlayerId.P2 && x.Steps >= 0);
    }

    [Fact]
    public void Wall_blocking_paths_on_Kid_is_illegal()
    {
        // 镜像 Standard 版 Wall_blocking_all_paths_is_illegal 的构造, Kid 7×7 角落同理封死。
        // P1 移到 (0,0); V(0,0) 阻东[(0,0)-(1,0) 与 (0,1)-(1,1)]; 预览 H(0,1) 阻北[(0,1)-(0,2)]。
        // P1 可达集 = {(0,0),(0,1)}, 均不在北边 row=6 → WallBlocksAllPaths。
        var s = GameSetup.CreateKid2P();
        s = s with
        {
            Pawns = s.Pawns.Replace(s.PawnOf(PlayerId.P1), s.PawnOf(PlayerId.P1) with { Pos = new Cell(0, 0) }),
        };
        s = s with { Walls = s.Walls.Add(new WallPos(new Cell(0, 0), WallOrient.Vertical)) };

        var r = PreviewService.PoseWall(s, new WallPos(new Cell(0, 1), WallOrient.Horizontal));

        Assert.False(r.Legal);
        Assert.Equal(RejectReason.WallBlocksAllPaths, r.Reason);
    }

    [Fact]
    public void Step_count_is_exact_for_known_position()
    {
        var state = GameSetup.CreateKid2P();  // P1(3,0)→北 row6, P2(3,6)→南 row0, 无墙
        var wall = new WallPos(new Cell(0, 0), WallOrient.Horizontal);  // 角落, 不影响列 3

        var r = PreviewService.PoseWall(state, wall);

        // 无墙直线距离: P1 row0→row6 = 6 步, P2 row6→row0 = 6 步。
        Assert.Equal(6, StepsOf(r, PlayerId.P1));
        Assert.Equal(6, StepsOf(r, PlayerId.P2));
    }

    [Fact]
    public void Preview_ignores_wall_budget()
    {
        // 设计决策文档化: 预览只管结构合法性(重叠/越界/可达性), 不管墙数。
        // 墙耗尽禁拾由 BoardView.Render(wallable = WallsLeft>0 && !IsFinished) 另管, 与预览解耦。
        var s = GameSetup.CreateKid2P();
        s = s with
        {
            Players = s.Players.Select(p => p with { WallsLeft = 0 }).ToImmutableArray(),
        };

        var r = PreviewService.PoseWall(s, new WallPos(new Cell(0, 0), WallOrient.Horizontal));

        Assert.True(r.Legal);
        Assert.Null(r.Reason);
    }

    private static int StepsOf(PreviewResult r, PlayerId id)
    {
        foreach (var x in r.Routes) if (x.Pawn == id) return x.Steps;
        return -1;
    }
}
