namespace Quoridor.Domain.Core;

public sealed record BoardConfig(int Size, BoardVariant Variant)
{
    public static BoardConfig Standard => new(9, BoardVariant.Standard);
    public static BoardConfig Kid => new(7, BoardVariant.Kid);
    public int MaxIndex => Size - 1;
}

public static class WallBudget
{
    public static int PerPlayer(BoardVariant variant, int playerCount) => variant switch
    {
        BoardVariant.Standard => playerCount == 2 ? 10 : 5,
        BoardVariant.Kid => playerCount == 2 ? 8 : 4,
        _ => throw new System.ArgumentOutOfRangeException(nameof(variant)),
    };
}
