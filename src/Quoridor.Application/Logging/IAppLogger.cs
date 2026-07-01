namespace Quoridor.Application.Logging;

public interface IAppLogger
{
    void Log(LogLevel level, string message, params object[] args);
}
