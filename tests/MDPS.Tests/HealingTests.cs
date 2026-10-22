using System.Text.Json;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using MDPS;

public class HealingTests
{
    private static CombatTracker Create()
    {
        var data = GameData.Empty;
        data.HealSkills.Add(17010000);
        var tracker = new CombatTracker(data, new MeterOptions { TargetMode = TargetMode.All });
        tracker.Process(new SelfIdentifiedEvent(1000, 1, "Curadora", 1, GameClass.Cleric));
        tracker.Process(new PlayerSeenEvent(1000, 2, "Tanque", 1, GameClass.Templar));
        tracker.Process(new NpcSeenEvent(1000, 900, 0, 100000));
        tracker.Process(new DamageEvent(1000, 2, 900, 12010000, 1000, HitFlags.None));
        tracker.Process(new DamageEvent(10000, 2, 900, 12010000, 1000, HitFlags.None));
        return tracker;
    }

    [Fact]
    public void HealerWithoutDamageGetsOutgoingHealingAndHpsOnly()
    {
        var t = Create();
        t.Process(new HealEvent(10000, 1, 2, 17010000, 6000, HitFlags.None));
        t.Process(new HealEvent(10000, 1, 1, 17010000, 3000, HitFlags.Dot));
        t.Process(new HealEvent(10000, 2, 1, 12010000, 7000, HitFlags.None));
        var s = t.Snapshot(null, 10000)!;
        var self = s.Combatants.Single(c => c.IsSelf);
        Assert.Equal(9000, self.Healing);
        Assert.Equal(1000, self.Hps);
        Assert.Equal(0, self.Damage);
        Assert.Equal(2000, s.TotalDamage);
        Assert.Equal(7000, s.Combatants.Single(c => !c.IsSelf).Healing);
    }

    [Fact]
    public void DirectAllyHealOnDamageOpcodeIsCountedAsHealing()
    {
        var t = Create();
        t.Process(new DamageEvent(10000, 1, 2, 17010042, 9000, HitFlags.Critical));
        var s = t.Snapshot(null, 10000)!;
        Assert.Equal(9000, s.Combatants.Single(c => c.IsSelf).Healing);
        Assert.Equal(2000, s.TotalDamage);
    }

    [Fact]
    public void HealingIsSavedAndResetBetweenFights()
    {
        var t = Create();
        FightRecord? saved = null;
        t.EncounterFinished += record => saved = record;
        t.Process(new HealEvent(10000, 1, 2, 17010000, 9000, HitFlags.None));
        t.Reset();
        Assert.NotNull(saved);
        var roundtrip = JsonSerializer.Deserialize<FightRecord>(JsonSerializer.Serialize(saved))!;
        Assert.Equal(9000, roundtrip.Summary.Combatants.Single(c => c.IsSelf).Healing);
        Assert.Contains(";9000;1000,00", CsvReport.Create(roundtrip.Summary, false));
        t.Process(new HealEvent(10500, 1, 2, 17010000, 5000, HitFlags.None)); // between fights
        t.Process(new DamageEvent(11000, 2, 900, 12010000, 1000, HitFlags.None));
        t.Process(new HealEvent(11000, 1, 2, 17010000, 2000, HitFlags.None));
        Assert.Equal(2000, t.Snapshot(null, 11000)!.Combatants.Single(c => c.IsSelf).Healing);
    }

    [Fact]
    public void SummonHealingBelongsToItsKnownOwner()
    {
        var t = Create();
        t.Process(new SummonSeenEvent(10000, 5, 1, null, 0));
        t.Process(new HealEvent(10000, 5, 2, 17010000, 2000, HitFlags.None));
        Assert.Equal(2000, t.Snapshot(null, 10000)!.Combatants.Single(c => c.IsSelf).Healing);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(50000001)]
    public void InvalidHealingIsIgnored(long amount)
    {
        var t = Create();
        t.Process(new HealEvent(10000, 1, 2, 17010000, amount, HitFlags.None));
        Assert.DoesNotContain(t.Snapshot(null, 10000)!.Combatants, c => c.IsSelf);
    }

    [Fact]
    public void OldHistoryKeepsHealingUnknownRatherThanZero()
    {
        var row = new CombatantSnapshot(1, "Antigo", GameClass.Cleric, true, 10, 10, 1, 1, 0, 10, 0);
        var json = JsonSerializer.SerializeToNode(row)!;
        json.AsObject().Remove("Healing");
        json.AsObject().Remove("Hps");
        var restored = json.Deserialize<CombatantSnapshot>()!;
        Assert.Null(restored.Healing);
        Assert.Null(restored.Hps);
    }
}
