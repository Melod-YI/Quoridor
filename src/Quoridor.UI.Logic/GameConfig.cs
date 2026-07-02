using Quoridor.Domain.AI;
using Quoridor.Domain.Core;

namespace Quoridor.UI.Logic;

public enum MatchMode { VsAi, HotSeat }

/// <summary>StartFrame → MainController → GameView 的开局契约。先手方通过座位顺序达成(见 SeatsBuilder)。</summary>
public sealed record GameConfig(
    BoardVariant Variant,        // Standard(9x9) | Kid(7x7)
    MatchMode Mode,              // VsAi | HotSeat
    Difficulty AiDifficulty,     // VsAi 时生效; HotSeat 时忽略
    PlayerId FirstMove);         // 先手方 P1 | P2
