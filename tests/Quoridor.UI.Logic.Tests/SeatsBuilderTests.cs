using Quoridor.Application.Seats;
using Quoridor.Domain.Core;
using Quoridor.UI.Logic;
using Xunit;

namespace Quoridor.UI.Logic.Tests;

public class SeatsBuilderTests
{
    [Fact]
    public void VsAi_first_P1_yields_P1_human_P2_ai()
    {
        var cfg = new GameConfig(BoardVariant.Standard, MatchMode.VsAi, Domain.AI.Difficulty.Medium, PlayerId.P1);
        var seats = SeatsBuilder.Build(cfg);
        Assert.Equal(PlayerId.P1, seats[0].Id);
        Assert.Equal(PlayerId.P2, seats[1].Id);
        Assert.True(seats[0].IsHuman);
        Assert.False(seats[1].IsHuman);
    }

    [Fact]
    public void VsAi_first_P2_swaps_seats_so_P1_is_ai()
    {
        var cfg = new GameConfig(BoardVariant.Standard, MatchMode.VsAi, Domain.AI.Difficulty.Medium, PlayerId.P2);
        var seats = SeatsBuilder.Build(cfg);
        Assert.Equal(PlayerId.P1, seats[0].Id);
        Assert.False(seats[0].IsHuman);
        Assert.True(seats[1].IsHuman);
    }

    [Fact]
    public void HotSeat_both_human_regardless_of_first()
    {
        var cfgP1 = new GameConfig(BoardVariant.Kid, MatchMode.HotSeat, Domain.AI.Difficulty.Easy, PlayerId.P1);
        var cfgP2 = new GameConfig(BoardVariant.Kid, MatchMode.HotSeat, Domain.AI.Difficulty.Easy, PlayerId.P2);
        foreach (var cfg in new[] { cfgP1, cfgP2 })
        {
            var seats = SeatsBuilder.Build(cfg);
            Assert.True(seats.All(s => s.IsHuman));
            Assert.Equal(2, seats.Count);
        }
    }

    [Fact]
    public void SeatMap_first_P1_maps_P1_to_player1()
    {
        var map = SeatMap.ForFirstMove(PlayerId.P1);
        Assert.Equal(1, map.ToDisplayNumber(PlayerId.P1));
        Assert.Equal(2, map.ToDisplayNumber(PlayerId.P2));
        Assert.Equal(PlayerId.P1, map.FromDisplayNumber(1));
    }

    [Fact]
    public void SeatMap_first_P2_swaps_display_numbers()
    {
        var map = SeatMap.ForFirstMove(PlayerId.P2);
        Assert.Equal(2, map.ToDisplayNumber(PlayerId.P1));
        Assert.Equal(1, map.ToDisplayNumber(PlayerId.P2));
        Assert.Equal(PlayerId.P2, map.FromDisplayNumber(1));
    }
}
