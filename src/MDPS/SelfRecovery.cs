using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;

namespace MDPS;

/// <summary>Recovers the last known local actor when the reader starts after the character loaded.</summary>
public static class SelfRecovery
{
    public static GameClass ClassFromRecentHistory(IEnumerable<FightRecord> history, string? name, int serverId, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name)) return GameClass.Unknown;
        foreach (var fight in history.OrderByDescending(f => f.Summary.StartedAt))
        {
            var age = now - fight.Summary.StartedAt;
            if (age < TimeSpan.Zero || age > TimeSpan.FromMinutes(45)) continue;
            var self = fight.Summary.Combatants.FirstOrDefault(c => c.IsSelf && c.Class != GameClass.Unknown
                && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)
                && (serverId == 0 || c.ServerId == 0 || serverId == c.ServerId));
            if (self is not null) return self.Class;
        }
        return GameClass.Unknown;
    }

    public static SessionState? FromRecentHistory(IEnumerable<FightRecord> history, DateTimeOffset now)
    {
        foreach (var fight in history.OrderByDescending(f => f.Summary.StartedAt))
        {
            var age = now - fight.Summary.StartedAt;
            if (age < TimeSpan.Zero || age > TimeSpan.FromMinutes(45)) continue;
            var self = fight.Summary.Combatants.FirstOrDefault(c => c.IsSelf && c.ActorId != 0 && !string.IsNullOrWhiteSpace(c.Name));
            if (self is null) continue;
            return new SessionState(self.ActorId, 0,
                [new CachedPlayer(self.ActorId, self.Name, self.Class, self.ServerId)], []);
        }
        return null;
    }
}
