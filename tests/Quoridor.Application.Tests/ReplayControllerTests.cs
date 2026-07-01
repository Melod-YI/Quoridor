using Quoridor.Domain.Core;
using Quoridor.Domain.Notation;
using Xunit;

namespace Quoridor.Application.Tests;

public class ReplayControllerTests
{
    private const string TwoPGame = "1. e2 e8 2. e3 e7";

    [Fact]
    public void Construct_parses_total_and_starts_at_initial_state()
    {
        var rc = new ReplayController(BoardConfig.Standard, 2, TwoPGame);
        Assert.Equal(4, rc.Total);
        Assert.True(rc.AtStart);
        Assert.False(rc.AtEnd);
        Assert.Equal(new Cell(4, 0), rc.Current.PawnOf(PlayerId.P1).Pos);  // 初始 e1
    }

    [Fact]
    public void Step_forward_advances_cursor_and_state()
    {
        var rc = new ReplayController(BoardConfig.Standard, 2, TwoPGame);
        Assert.True(rc.StepForward());
        Assert.Equal(1, rc.Cursor);
        Assert.Equal(new Cell(4, 1), rc.Current.PawnOf(PlayerId.P1).Pos);  // e2
    }

    [Fact]
    public void At_end_step_forward_returns_false()
    {
        var rc = new ReplayController(BoardConfig.Standard, 2, TwoPGame);
        for (int i = 0; i < 4; i++) rc.StepForward();
        Assert.True(rc.AtEnd);
        Assert.False(rc.StepForward());
    }

    [Fact]
    public void Step_back_rebuilds_state_to_prior_cursor()
    {
        var rc = new ReplayController(BoardConfig.Standard, 2, TwoPGame);
        rc.GoTo(3);  // 走到第 3 手后
        Assert.Equal(3, rc.Cursor);

        Assert.True(rc.StepBack());

        Assert.Equal(2, rc.Cursor);
        // 第 2 手后: P1 在 e2(已走第1手), P2 在 e8(已走第2手)
        Assert.Equal(new Cell(4, 1), rc.Current.PawnOf(PlayerId.P1).Pos);
        Assert.Equal(new Cell(4, 7), rc.Current.PawnOf(PlayerId.P2).Pos);
    }

    [Fact]
    public void Reset_returns_to_start()
    {
        var rc = new ReplayController(BoardConfig.Standard, 2, TwoPGame);
        rc.GoTo(4);
        rc.Reset();
        Assert.True(rc.AtStart);
        Assert.Equal(0, rc.Cursor);
    }

    [Fact]
    public void Go_to_jump_arbitrary_index()
    {
        var rc = new ReplayController(BoardConfig.Standard, 2, TwoPGame);
        rc.GoTo(2);
        Assert.Equal(2, rc.Cursor);
        Assert.Equal(new Cell(4, 1), rc.Current.PawnOf(PlayerId.P1).Pos);
        Assert.Equal(new Cell(4, 7), rc.Current.PawnOf(PlayerId.P2).Pos);
        Assert.Equal(PlayerId.P1, rc.Current.ActivePlayer);
    }

    [Fact]
    public void Invalid_replay_move_throws_on_step()
    {
        // 记谱 5 手: 前 4 手合法(e2,e8,e3,e7), 第 5 手 e3 非法(P1 已在 e3, 停留原地被 RuleEngine 拒绝)
        var rc = new ReplayController(BoardConfig.Standard, 2, "1. e2 e8 2. e3 e7 3. e3");
        rc.StepForward(); rc.StepForward(); rc.StepForward(); rc.StepForward();  // 走完 4 手合法部分
        Assert.Equal(5, rc.Total);
        Assert.Throws<NotationParseException>(() => rc.StepForward());  // 第5手 e3 非法
    }

    [Fact]
    public void Step_back_at_start_returns_false()
    {
        var rc = new ReplayController(BoardConfig.Standard, 2, TwoPGame);
        Assert.False(rc.StepBack());  // cursor=0 无可回退
        Assert.True(rc.AtStart);
    }

    [Fact]
    public void Go_to_zero_resets_to_start()
    {
        var rc = new ReplayController(BoardConfig.Standard, 2, TwoPGame);
        rc.GoTo(3);
        rc.GoTo(0);
        Assert.True(rc.AtStart);
        Assert.Equal(0, rc.Cursor);
        Assert.Equal(new Cell(4, 0), rc.Current.PawnOf(PlayerId.P1).Pos);
    }

    [Fact]
    public void Go_to_total_reaches_end()
    {
        var rc = new ReplayController(BoardConfig.Standard, 2, TwoPGame);
        rc.GoTo(rc.Total);
        Assert.True(rc.AtEnd);
        Assert.Equal(rc.Total, rc.Cursor);
    }

    [Fact]
    public void Go_to_out_of_range_throws()
    {
        var rc = new ReplayController(BoardConfig.Standard, 2, TwoPGame);
        Assert.Throws<ArgumentOutOfRangeException>(() => rc.GoTo(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => rc.GoTo(rc.Total + 1));
    }
}
