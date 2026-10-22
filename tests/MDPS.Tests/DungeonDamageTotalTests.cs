using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using MDPS;

public class DungeonDamageTotalTests
{
    private static EncounterSnapshot Fight(Guid id, string title, long damage, long healing, bool active) =>
        new(id, title, "DG", DateTimeOffset.UtcNow, 10000, 10000, active,
            active ? EncounterEndReason.None : EncounterEndReason.Kill,
            damage, damage / 10.0, null,
            [new CombatantSnapshot(1, "Eu", GameClass.Cleric, true, damage, damage / 10.0,
                1, 1, 0, damage, 0, Healing: healing)]);

    [Fact]
    public void MiniBossKeepsEarlierDamageWithoutDoubleCountingFinishedFight()
    {
        var total = new DungeonDamageTotal();
        var first = Fight(Guid.NewGuid(), "Mini boss", 10000, 3000, false);
        total.AddFinished(new FightRecord(1, first, []));
        total.AddFinished(new FightRecord(1, first, []));
        var boss = Fight(Guid.NewGuid(), "Boss", 5000, 2000, true);
        var combined = total.Snapshot(boss)!;
        Assert.Equal(15000, combined.TotalDamage);
        Assert.Equal(15000, Assert.Single(combined.Combatants).Damage);
        Assert.Equal(5000, Assert.Single(combined.Combatants).Healing);
        Assert.Equal(750, combined.PartyDps);
        Assert.Equal("Boss", combined.Title);
        total.NewSession();
        Assert.Null(total.Snapshot(null));
    }
}
