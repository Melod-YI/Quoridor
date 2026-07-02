using Quoridor.Domain.Core;

namespace Quoridor.UI.Logic;

public static class RejectReasonText
{
    public static string Of(RejectReason r) => r switch
    {
        RejectReason.NotYourTurn => "还没轮到你",
        RejectReason.IllegalMove => "非法走子",
        RejectReason.BlockedByWall => "被墙挡住",
        RejectReason.OffBoard => "越出棋盘",
        RejectReason.WallOverlap => "墙位重叠",
        RejectReason.WallPlusIntersection => "墙与十字交叉冲突",
        RejectReason.WallOutOfBounds => "墙越界",
        RejectReason.WallBlocksAllPaths => "此墙会封死某方路径",
        RejectReason.NoWallsLeft => "墙已用完",
        RejectReason.GameFinished => "对局已结束",
        _ => r.ToString(),
    };
}
