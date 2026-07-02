using Quoridor.UI.Logic;
using Xunit;

namespace Quoridor.UI.Logic.Tests;

public class NamedPlaceholderTests
{
    [Fact]
    public void Named_tokens_filled_by_position()
    {
        Assert.Equal("cfg=K players=2", NamedPlaceholder.Format("cfg={Variant} players={N}", "K", 2));
    }

    [Fact]
    public void Escaped_braces_literal()
    {
        Assert.Equal("{x}", NamedPlaceholder.Format("{{x}}", 1));
    }

    [Fact]
    public void More_placeholders_than_args_keeps_remaining_token()
    {
        // 位置填充: 第一个占位符 {X} 由第一个 arg(2) 填充, 第二个 {Y} 无 arg 保留字面
        Assert.Equal("a=2 b={Y}", NamedPlaceholder.Format("a={X} b={Y}", 2));
    }

    [Fact]
    public void More_args_than_placeholders_ignores_extra()
    {
        Assert.Equal("only=1", NamedPlaceholder.Format("only={A}", 1, 2, 3));
    }

    [Fact]
    public void Empty_message_no_args()
    {
        Assert.Equal("", NamedPlaceholder.Format(""));
    }
}
