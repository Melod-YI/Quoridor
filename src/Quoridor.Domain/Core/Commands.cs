namespace Quoridor.Domain.Core;

public interface IGameCommand;
public sealed record MovePawnCommand(Cell To) : IGameCommand;
public sealed record PlaceWallCommand(WallPos Wall) : IGameCommand;
