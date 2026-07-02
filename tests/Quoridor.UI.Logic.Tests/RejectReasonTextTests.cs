using Quoridor.Domain.Core;
using Quoridor.UI.Logic;
using Xunit;

namespace Quoridor.UI.Logic.Tests;

public class RejectReasonTextTests
{
    [Fact]
    public void Every_reason_has_nonempty_text()
    {
        foreach (RejectReason r in Enum.GetValues(typeof(RejectReason)))
            Assert.False(string.IsNullOrWhiteSpace(RejectReasonText.Of(r)));
    }

    [Fact]
    public void NoWallsLeft_has_meaningful_text()
    {
        Assert.Contains("墙", RejectReasonText.Of(RejectReason.NoWallsLeft));
    }
}
