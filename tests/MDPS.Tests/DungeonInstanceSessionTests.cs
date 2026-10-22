using AionMeter.Core.Events;
using MDPS;

public class DungeonInstanceSessionTests
{
    [Fact]
    public void A_new_countdown_resets_when_reentering_the_same_dungeon()
    {
        var detector = new DungeonInstanceSession();

        Assert.False(detector.Observe(new InstanceTimerEvent(1_000, 600092, 1, 10_000, 40_000)));
        Assert.False(detector.Observe(new InstanceTimerEvent(2_000, 600092, 1, 10_000, 40_000)));
        Assert.True(detector.Observe(new InstanceTimerEvent(60_000, 600092, 1, 70_000, 100_000)));
    }

    [Fact]
    public void Reloading_after_death_keeps_the_same_dungeon_total()
    {
        var detector = new DungeonInstanceSession();

        Assert.False(detector.Observe(new InstanceTimerEvent(1_000, 600092, 2, 10_000, 3_610_000)));
        Assert.False(detector.Observe(new InstanceTimerEvent(90_000, 600092, 2, 10_000, 3_610_000)));
    }

    [Fact]
    public void Boss_countdowns_do_not_start_a_new_dungeon_session()
    {
        var detector = new DungeonInstanceSession();

        Assert.False(detector.Observe(new InstanceTimerEvent(1_000, 600092, InstanceTimerEvent.BossLimit, 1_000, 61_000)));
        Assert.False(detector.Observe(new InstanceTimerEvent(2_000, 600092, InstanceTimerEvent.BossLimit, 2_000, 62_000)));
    }
}
