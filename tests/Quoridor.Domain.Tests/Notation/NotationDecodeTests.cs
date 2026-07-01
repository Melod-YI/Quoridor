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
        // 非法 token "z9" 列越界（z 远超棋盘），Replay 时由规则拒绝
        Assert.Throws<NotationParseException>(() =>
            NotationService.Replay(BoardConfig.Standard, 2, "1. z9 e8"));
    }
}
