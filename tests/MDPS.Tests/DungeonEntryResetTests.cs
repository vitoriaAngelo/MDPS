using AionMeter.Core.Events;
using MDPS;

public class DungeonEntryResetTests
{
    [Theory]
    [InlineData(110002, 600092, true, false, true)]
    [InlineData(0, 600092, true, false, true)]
    [InlineData(600021, 600092, true, false, false)]
    [InlineData(600092, 600092, true, false, false)]
    [InlineData(600092, 600092, true, true, false)]
    [InlineData(610062, 610063, true, false, false)]
    [InlineData(610062, 610071, true, false, false)]
    [InlineData(610062, 110002, false, false, true)]
    [InlineData(110002, 110003, false, false, false)]
    public void Only_entering_a_new_dungeon_clears_the_panel(int previousMap, int nextMap,
        bool dungeon, bool teleport, bool expected)
    {
        var zone = new ZoneChangedEvent(1000, nextMap, "Mapa", dungeon, teleport);
        Assert.Equal(expected, DungeonEntryReset.ShouldReset(previousMap, zone));
    }
}
