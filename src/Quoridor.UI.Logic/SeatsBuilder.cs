using Quoridor.Application.Seats;
using Quoridor.Domain.Core;

namespace Quoridor.UI.Logic;

public static class SeatsBuilder
{
    /// <summary>返回 [P1 座位, P2 座位]。P1 始终是 Domain 起手(GameSetup 硬编码 ActivePlayer=P1)。
    /// 先手=P1: P1=玩家1, P2=玩家2; 先手=P2: P1=玩家2(先手), P2=玩家1(后手)。
    /// VsAi: 人类=玩家1, AI=玩家2(固定身份); 故先手=P2 时 P1 座位=AI(先手), P2 座位=人类(后手)。</summary>
    public static IReadOnlyList<IPlayer> Build(GameConfig cfg)
    {
        if (cfg.Mode == MatchMode.HotSeat)
            return new IPlayer[] { new HumanPlayer(PlayerId.P1), new HumanPlayer(PlayerId.P2) };

        // VsAi: 人类=玩家1, AI=玩家2
        if (cfg.FirstMove == PlayerId.P1)
            return new IPlayer[] { new HumanPlayer(PlayerId.P1), AiPlayerFactory.Create(PlayerId.P2, cfg.AiDifficulty) };
        else
            return new IPlayer[] { AiPlayerFactory.Create(PlayerId.P1, cfg.AiDifficulty), new HumanPlayer(PlayerId.P2) };
    }
}
