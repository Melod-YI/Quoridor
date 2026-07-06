using Quoridor.Domain.AI;
using Quoridor.Domain.Core;

namespace Quoridor.UI.Logic;

public enum MatchMode { VsAi, HotSeat, Replay }

/// <summary>StartFrame → MainController → GameView 的开局契约。
/// Replay 模式: Replay 非 null, Variant 取自 Replay.Variant, AiDifficulty/FirstMove 忽略。</summary>
public sealed record GameConfig(
    BoardVariant Variant,
    MatchMode Mode,
    Difficulty AiDifficulty,
    PlayerId FirstMove,
    ReplayEntry? Replay = null);
