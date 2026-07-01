using Quoridor.Application.Logging;

namespace Quoridor.Application.Tests.Logging;

public class NullAppLoggerTests
{
    [Fact]
    public void Null_logger_swallows_messages_without_throwing()
    {
        IAppLogger logger = new NullAppLogger();
        logger.Log(LogLevel.Info, "anything {0}", 1);
        logger.Log(LogLevel.Error, "boom");
    }

    [Fact]
    public void Capturing_logger_receives_message_via_interface_contract()
    {
        // 用一个测试专用捕获实现, 验证 IAppLogger 契约把消息传给实现者
        var cap = new CapturingLogger();
        cap.Log(LogLevel.Warning, "hi");
        Assert.Single(cap.Messages);
        Assert.Equal("hi", cap.Messages[0]);
        Assert.Equal(LogLevel.Warning, cap.Levels[0]);
    }

    private sealed class CapturingLogger : IAppLogger
    {
        public List<string> Messages { get; } = new();
        public List<LogLevel> Levels { get; } = new();

        public void Log(LogLevel level, string message, params object[] args)
        {
            Levels.Add(level);
            Messages.Add(message);
        }
    }
}
