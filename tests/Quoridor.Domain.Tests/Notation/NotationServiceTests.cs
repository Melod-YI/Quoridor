using Quoridor.Domain.Core;
using Quoridor.Domain.Notation;
using Quoridor.Domain.Rules;
using Xunit;

namespace Quoridor.Domain.Tests.Notation;

public class NotationServiceTests
{
    [Fact]
    public void Cell_and_wall_to_notation()
    {
        Assert.Equal("e2", NotationService.CellToNotation(new Cell(4, 1)));
        Assert.Equal("e3v", NotationService.WallToNotation(new WallPos(new Cell(4, 2), WallOrient.Vertical)));
        Assert.Equal("a1", NotationService.CellToNotation(new Cell(0, 0)));
        Assert.Equal("i9", NotationService.CellToNotation(new Cell(8, 8)));
    }

    [Fact]
    public void Encode_two_player_sequence()
    {
        // P1: e1->e2 ; P2: e9->e8 ; P1: e2->e3 ; P2: e8->e7
        var s = GameSetup.CreateStandard2P();
        var events = new System.Collections.Generic.List<IGameEvent>();
        s = Apply(s, new MovePawnCommand(new Cell(4, 1)), events);
        s = Apply(s, new MovePawnCommand(new Cell(4, 7)), events);
        s = Apply(s, new MovePawnCommand(new Cell(4, 2)), events);
        s = Apply(s, new MovePawnCommand(new Cell(4, 6)), events);

        Assert.Equal("1. e2 e8 2. e3 e7", NotationService.Encode(events, 2));
    }

    [Fact]
    public void Encode_four_player_sequence()
    {
        var s = GameSetup.CreateStandard4P();
        var events = new System.Collections.Generic.List<IGameEvent>();
        s = Apply(s, new MovePawnCommand(new Cell(4, 1)), events); // P1 e2
        s = Apply(s, new MovePawnCommand(new Cell(1, 4)), events); // P2 b5
        s = Apply(s, new MovePawnCommand(new Cell(4, 7)), events); // P3 e8
        s = Apply(s, new MovePawnCommand(new Cell(7, 4)), events); // P4 h5

        Assert.Equal("1. e2 b5 e8 h5", NotationService.Encode(events, 4));
    }

    [Fact]
    public void Encode_includes_wall_ply()
    {
        var s = GameSetup.CreateStandard2P();
        var events = new System.Collections.Generic.List<IGameEvent>();
        s = Apply(s, new PlaceWallCommand(new WallPos(new Cell(4, 2), WallOrient.Vertical)), events); // e3v
        s = Apply(s, new MovePawnCommand(new Cell(4, 7)), events); // P2 e8

        Assert.Equal("1. e3v e8", NotationService.Encode(events, 2));
    }

    private static GameState Apply(GameState s, IGameCommand c, System.Collections.Generic.List<IGameEvent> ev)
    {
        var r = RuleEngine.ValidateAndApply(s, c);
        Assert.NotNull(r.State);
        ev.AddRange(r.Events);
        return r.State!;
    }
}
