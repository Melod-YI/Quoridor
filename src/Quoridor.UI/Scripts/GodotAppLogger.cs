using System.Collections.Generic;
using Godot;
using Quoridor.Application.Logging;
using Quoridor.UI.Logic;

namespace Quoridor.UI;

/// <summary>把 Application 的 IAppLogger 桥到 Godot: 命名占位符替换后 GD.Print/PushWarning/PushError。
/// 同时写内存 ring buffer 供手动验收事后核查(见 spec §9.2)。</summary>
public sealed class GodotAppLogger : IAppLogger
{
    private readonly Queue<string> _warnings = new();
    private const int BufferCap = 256;

    public IReadOnlyCollection<string> RecentWarnings => _warnings;

    public void Log(LogLevel level, string message, params object[] args)
    {
        string formatted = NamedPlaceholder.Format(message, args);
        switch (level)
        {
            case LogLevel.Debug:
            case LogLevel.Info:
                GD.Print(formatted);
                break;
            case LogLevel.Warning:
                GD.PushWarning(formatted);
                EnqueueWarning(formatted);
                break;
            case LogLevel.Error:
                GD.PushError(formatted);
                EnqueueWarning(formatted);
                break;
        }
    }

    private void EnqueueWarning(string s)
    {
        _warnings.Enqueue(s);
        while (_warnings.Count > BufferCap) _warnings.Dequeue();
    }
}
