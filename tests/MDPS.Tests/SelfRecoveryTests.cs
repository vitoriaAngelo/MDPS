using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using MDPS;

public class SelfRecoveryTests
{
    [Fact]
    public void Recent_self_identity_can_be_restored_after_restarting_mid_map()
    {
        var now = DateTimeOffset.UtcNow;
        var self = new CombatantSnapshot(12603, "Mistty", GameClass.Cleric, true,
            75636, 13274, 1, 10, 0, 10000, 0, 2404);
        var summary = new EncounterSnapshot(Guid.NewGuid(), "Kailin", "Open world", now.AddMinutes(-20),
            5000, 5000, false, EncounterEndReason.Idle, 75636, 13274, null, [self]);
        var record = new FightRecord(1, summary, []);

        var restored = SelfRecovery.FromRecentHistory([record], now)!;
        Assert.Equal((uint)12603, restored.SelfId);
        Assert.Equal("Mistty", Assert.Single(restored.Players).Name);
        Assert.Null(SelfRecovery.FromRecentHistory([record], now.AddHours(1)));
        Assert.Equal(GameClass.Cleric, SelfRecovery.ClassFromRecentHistory([record], "Mistty", 2404, now));
        Assert.Equal(GameClass.Unknown, SelfRecovery.ClassFromRecentHistory([record], "Outra", 2404, now));
        Assert.Equal(GameClass.Unknown, SelfRecovery.ClassFromRecentHistory([record], "Mistty", 9000, now));
    }
}
