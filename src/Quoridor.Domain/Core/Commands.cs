namespace Quoridor.Domain.Core;

public interface IGameCommand;
public sealed record MovePawnCommand(Cell To) : IGameCommand;
public sealed record PlaceWallCommand(WallPos Wall) : IGameCommand;
/// <summary>投降: 当前活跃玩家认输。仅 2 人 MVP 语义——对方获胜。</summary>
public sealed record SurrenderCommand : IGameCommand;
