using Quoridor.Domain.Core;

namespace Quoridor.UI.Logic;

/// <summary>Domain PlayerId ↔ 显示玩家编号 的映射。先手=P2 时 P1 座位=玩家2, P2 座位=玩家1。</summary>
public readonly record struct SeatMap(PlayerId FirstMove)
{
    public static SeatMap ForFirstMove(PlayerId first) => new(first);

    public int ToDisplayNumber(PlayerId id) =>
        FirstMove == PlayerId.P1
            ? (id == PlayerId.P1 ? 1 : 2)
            : (id == PlayerId.P1 ? 2 : 1);

    public PlayerId FromDisplayNumber(int display) =>
        FirstMove == PlayerId.P1
            ? (display == 1 ? PlayerId.P1 : PlayerId.P2)
            : (display == 1 ? PlayerId.P2 : PlayerId.P1);
}
