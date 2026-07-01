namespace Quoridor.Domain.Core;

public readonly record struct Cell(int Col, int Row) : IComparable<Cell>
{
    public int CompareTo(Cell other)
    {
        int c = Col.CompareTo(other.Col);
        return c != 0 ? c : Row.CompareTo(other.Row);
    }
    public static bool operator <(Cell a, Cell b) => a.CompareTo(b) < 0;
    public static bool operator >(Cell a, Cell b) => a.CompareTo(b) > 0;
    public static bool operator <=(Cell a, Cell b) => a.CompareTo(b) <= 0;
    public static bool operator >=(Cell a, Cell b) => a.CompareTo(b) >= 0;
}
