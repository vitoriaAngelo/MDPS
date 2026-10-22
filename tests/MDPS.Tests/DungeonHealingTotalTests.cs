using AionMeter.Core.Events;
using MDPS;

public class DungeonHealingTotalTests
{
    [Fact]
    public void HealingContinuesAcrossBossFightsUntilNewSession()
    {
        var total = new DungeonHealingTotal();
        total.ObserveHeal(new HealEvent(1000, 1, 2, 1, 6000, HitFlags.None), 1);
        total.ObserveHeal(new HealEvent(2000, 1, 3, 1, 4000, HitFlags.None), 1);
        total.ObserveHeal(new HealEvent(3000, 4, 1, 1, 9000, HitFlags.None), 1);
        Assert.Equal(10000, total.Total());
        total.NewSession();
        Assert.Equal(0, total.Total());
    }
}
