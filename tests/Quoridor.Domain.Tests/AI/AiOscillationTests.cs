using Quoridor.Domain.AI;
using Quoridor.Domain.Core;
using Quoridor.Domain.Notation;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Domain.Tests.AI;

/// <summary>
/// 复现: Standard Hard vs Medium 记谱, P1(hard) 墙耗尽后在 c8↔d8 反复踱步。
/// 根因: 评估等分 + 严格大于不换 → 取枚举第一格 → 对称局面下跨回合 2-格环。
/// 修复(A): AI 实例累积己方历史位置, 根节点候选走回站过的格子 → 减分。
/// </summary>
public class AiOscillationTests
{
    // 取自 ReplayLibrary "Standard · Hard vs Medium" (72 ply, P2 胜)。
    private const string StandardHardVsMedium =
        "1. e2 e8 2. e3 e7 3. e4 e6 4. d2h e7h 5. f2h d8h 6. b2h e5 " +
        "7. e6 e4 8. d3v h2v 9. h1h e5v 10. e4h f4 11. g1v g3v 12. f6h g4 " +
        "13. a1h g5 14. g7v g6 15. e7 h6 16. d7 h7 17. d8 h8 18. c8 b8h " +
        "19. d8 h9 20. c8 g9 21. d8 g8 22. c8 g7 23. d8 f7 24. c8 e7 " +
        "25. d8 e6 26. c8 e5 27. d8 d5 28. c8 d4 29. d8 d3 30. c8 c3 " +
        "31. d8 b3 32. c8 a3 33. d8 a2 34. c8 b2 35. d8 c2 36. e8 c1";

    [Fact]
    public void Hard_does_not_step_back_to_recently_visited_cell()
    {
        var cmds = NotationService.Decode(StandardHardVsMedium, BoardConfig.Standard);

        // 还原到 ply36 后: P1 在 c8, P2 刚放 b8h, 轮 P1, P1 墙耗尽。
        var state = GameSetup.CreateStandard2P();
        for (int i = 0; i < 36; i++)
        {
            var r = RuleEngine.ValidateAndApply(state, cmds[i]);
            Assert.NotNull(r.State);
            state = r.State!;
        }
        var c8 = new Cell(2, 7);
        Assert.Equal(c8, state.PawnOf(PlayerId.P1).Pos);
        Assert.Equal(0, state.PlayerOf(PlayerId.P1).WallsLeft);

        var ai = new MinimaxAi();

        // s36 → P1 决策(c8→某邻格, 记 c8 到历史)。墙耗尽故必为走子。
        var cmd1 = Assert.IsType<MovePawnCommand>(ai.Choose(state, Difficulty.Hard));
        state = Apply(state, cmd1);
        Assert.NotEqual(c8, cmd1.To);  // 不应原地不动

        // ply38: P2 按原局记谱走 h9 (远离 P1, 不撞, 必合法)。到 s38: P1 仍轮到决策。
        state = Apply(state, cmds[37]);

        // s38: P1 决策, 历史含 c8(上步起点)。候选若含 c8(回头步) 应被 A 惩罚 → 不选。
        // A 前: 等分取枚举第一 → 选 c8(回头) → RED。
        // A 后: c8 在历史 → 减分 → 选其他邻格 → GREEN。
        var cmd2 = Assert.IsType<MovePawnCommand>(ai.Choose(state, Difficulty.Hard));
        Assert.NotEqual(c8, cmd2.To);
    }

    private static GameState Apply(GameState s, IGameCommand cmd)
    {
        var r = RuleEngine.ValidateAndApply(s, cmd);
        Assert.NotNull(r.State);
        return r.State!;
    }
}
