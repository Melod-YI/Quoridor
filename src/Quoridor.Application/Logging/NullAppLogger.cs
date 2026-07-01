namespace Quoridor.Application.Logging;

public sealed class NullAppLogger : IAppLogger
{
    public static NullAppLogger Instance { get; } = new();

    public void Log(LogLevel level, string message, params object[] args) { }
}
