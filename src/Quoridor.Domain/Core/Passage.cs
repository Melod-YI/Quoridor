namespace Quoridor.Domain.Core;

public readonly record struct Passage(Cell A, Cell B)
{
    public static Passage Between(Cell x, Cell y) =>
        x.CompareTo(y) <= 0 ? new Passage(x, y) : new Passage(y, x);
}
