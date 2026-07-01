using Quoridor.Domain.Core;

namespace Quoridor.Application.Seats;

/// <summary>座位门槛: 人与 AI 都实现"在给定状态产出一个命令(或 null 表示等待外部输入)"的契约。</summary>
public interface IPlayer
{
    PlayerId Id { get; }
    bool IsHuman { get; }

    /// <summary>返回该座位的拟走命令; 人类返回 null(经 GameSession.Submit 提交), AI 返回 IQuoridorAi.Choose 结果。</summary>
    IGameCommand? ProposeMove(GameState state);
}
