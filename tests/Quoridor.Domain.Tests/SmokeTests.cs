using Xunit;

namespace Quoridor.Domain.Tests;

public class SmokeTests
{
    [Fact]
    public void TestHarnessRuns()
    {
        Assert.Equal(4, 2 + 2);
    }
}
