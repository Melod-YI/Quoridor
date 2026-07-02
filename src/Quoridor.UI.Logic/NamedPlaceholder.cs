using System.Text;

namespace Quoridor.UI.Logic;

/// <summary>把 Application 日志的命名占位符 {Name} 按 args 顺序填充。不可用 string.Format(命名 token 会抛)。</summary>
public static class NamedPlaceholder
{
    public static string Format(string message, params object[] args)
    {
        if (string.IsNullOrEmpty(message)) return message ?? string.Empty;
        var sb = new StringBuilder();
        int idx = 0;
        int i = 0;
        while (i < message.Length)
        {
            if (message[i] == '{' && i + 1 < message.Length && message[i + 1] == '{')
            { sb.Append('{'); i += 2; continue; }
            if (message[i] == '}' && i + 1 < message.Length && message[i + 1] == '}')
            { sb.Append('}'); i += 2; continue; }
            if (message[i] == '{')
            {
                int close = message.IndexOf('}', i + 1);
                if (close > i)
                {
                    if (idx < args.Length)
                    { sb.Append(args[idx]?.ToString() ?? ""); idx++; }
                    else
                    { sb.Append(message, i, close - i + 1); }
                    i = close + 1;
                    continue;
                }
            }
            sb.Append(message[i]); i++;
        }
        return sb.ToString();
    }
}
