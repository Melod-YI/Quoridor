using Quoridor.Domain.Core;
using Quoridor.Domain.Notation;
using Xunit;

namespace Quoridor.Domain.Tests.Notation;

public class NotationDecodeTests
{
    [Fact]
    public void Decode_parses_moves_and_walls()
    {
        var cmds = NotationService.Decode("1. e2 e3v e8");
        Assert.Equal(3, cmds.Length);
        Assert.Equal(new Cell(4, 1), ((MovePawnCommand)cmds[0]).To);
        Assert.Equal(new WallPos(new Cell(4, 2), WallOrient.Vertical), ((PlaceWallCommand)cmds[1]).Wall);
        Assert.Equal(new Cell(4, 7), ((MovePawnCommand)cmds[2]).To);
    }

    [Fact]
    public void Decode_handles_inline_and_spaced_round_markers()
    {
        var a = NotationService.Decode("1.e2 1.e8");
        Assert.Equal(2, a.Length);
        var b = NotationService.Decode("1. e2 e8 2. e3 e7");
        Assert.Equal(4, b.Length);
    }

    [Fact]
    public void Replay_roundtrips_two_player_game()
    {
        var notation = "1. e2 e8 2. e3 e7";
        var final = NotationService.Replay(BoardConfig.Standard, 2, notation);
        Assert.Equal(new Cell(4, 2), final.PawnOf(PlayerId.P1).Pos);
        Assert.Equal(new Cell(4, 6), final.PawnOf(PlayerId.P2).Pos);
        Assert.Equal(PlayerId.P1, final.ActivePlayer);
    }

    [Fact]
    public void Decode_invalid_token_throws_parse_error()
    {
        // 债1: z9 列越界, Replay 内部用 cfg 感知 Decode → 抛精确"列越界"异常
        var ex = Assert.Throws<NotationParseException>(() =>
            NotationService.Replay(BoardConfig.Standard, 2, "1. z9 e8"));
        Assert.Contains("列越界", ex.Message);
    }

    [Fact]
    public void Decode_with_cfg_rejects_out_of_range_column()
    {
        // 债1: z9 列越界, cfg 感知解析应抛精确异常而非靠规则拒绝
        var ex = Assert.Throws<NotationParseException>(() =>
            NotationService.Decode("1. z9 e8", BoardConfig.Standard));
        Assert.Contains("列越界", ex.Message);
    }

    [Fact]
    public void Decode_with_cfg_rejects_out_of_range_row()
    {
        // e10 → row=9 越出 9×9(MaxIndex=8)
        var ex = Assert.Throws<NotationParseException>(() =>
            NotationService.Decode("1. e10", BoardConfig.Standard));
        Assert.Contains("行越界", ex.Message);
    }

    [Fact]
    public void Decode_handles_continuation_marker()  // 债2: 3... e3h
    {
        var cmds = NotationService.Decode("3... e3h");
        var wall = Assert.Single(cmds);
        var pw = Assert.IsType<PlaceWallCommand>(wall);
        Assert.Equal(new WallPos(new Cell(4, 2), WallOrient.Horizontal), pw.Wall);
    }

    [Fact]
    public void Decode_continuation_marker_with_both_sides()
    {
        // 3. e6h(白) 3... e3h(黑续谱)
        var cmds = NotationService.Decode("3. e6h 3... e3h");
        Assert.Equal(2, cmds.Length);
        Assert.All(cmds, c => Assert.IsType<PlaceWallCommand>(c));
    }

    [Fact]
    public void Decode_pure_continuation_marker_yields_no_commands()
    {
        Assert.Empty(NotationService.Decode("3..."));
        Assert.Empty(NotationService.Decode("3."));
    }
}
