namespace Quoridor.UI.Logic;

public enum SlotEdge { Vertical, Horizontal }

public readonly record struct SlotId(SlotEdge Edge, int Col, int Row);
