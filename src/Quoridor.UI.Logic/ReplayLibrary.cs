using Quoridor.Domain.AI;
using Quoridor.Domain.Core;

namespace Quoridor.UI.Logic;

/// <summary>一条预置回放棋局: P1=先手方(P1Diff), P2=后手方(P2Diff)。Notation 为完整记谱串。</summary>
public sealed record ReplayEntry(
    string Name,
    BoardVariant Variant,
    Difficulty P1Diff,
    Difficulty P2Diff,
    PlayerId Winner,
    int Plies,
    string Notation);

/// <summary>预置 AI vs AI 棋局库(18 条 = 9 难度组合 × 2 变体)。由 demo --gen-replays 产出。</summary>
public static class ReplayLibrary
{
    public static IReadOnlyList<ReplayEntry> All { get; } = System.Array.Empty<ReplayEntry>();
}
