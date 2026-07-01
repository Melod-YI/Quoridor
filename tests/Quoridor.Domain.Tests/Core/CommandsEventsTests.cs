using Quoridor.Domain.Core;
using Xunit;

namespace Quoridor.Domain.Tests.Core;

public class CommandsEventsTests
{
    [Fact]
    public void Commands_are_distinct_types()
    {
        IGameCommand a = new MovePawnCommand(new Cell(4, 1));
        IGameCommand b = new PlaceWallCommand(new WallPos(new Cell(0, 0), WallOrient.Horizontal));
        Assert.IsType<MovePawnCommand>(a);
        Assert.IsType<PlaceWallCommand>(b);
    }

    [Fact]
    public void Events_carry_player_and_reason()
    {
        IGameEvent e = new WallRejected(PlayerId.P1, new WallPos(new Cell(0, 0), WallOrient.Horizontal), RejectReason.WallOverlap);
        var wr = Assert.IsType<WallRejected>(e);
        Assert.Equal(PlayerId.P1, wr.Who);
        Assert.Equal(RejectReason.WallOverlap, wr.Reason);
    }
}
