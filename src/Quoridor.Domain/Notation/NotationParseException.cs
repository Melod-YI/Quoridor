using System;

namespace Quoridor.Domain.Notation;

public sealed class NotationParseException : Exception
{
    public NotationParseException(string message) : base(message) { }
}
