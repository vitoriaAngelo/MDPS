using AionMeter.Core.Combat;
using AionMeter.Core.Events;

namespace MDPS;

/// <summary>Combines combat segments into one total for the current dungeon session.</summary>
public sealed class DungeonDamageTotal
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, EncounterSnapshot> _finished = [];
    private Guid _sessionId = Guid.NewGuid();

    public void NewSession()
    {
        lock (_gate)
        {
            _finished.Clear();
            _sessionId = Guid.NewGuid();
        }
    }

    public void AddFinished(FightRecord record)
    {
        lock (_gate) _finished[record.Summary.Id] = record.Summary;
    }

    public EncounterSnapshot? Snapshot(EncounterSnapshot? current)
    {
        EncounterSnapshot[] segments;
        Guid sessionId;
        lock (_gate)
        {
            segments = _finished.Values.ToArray();
            sessionId = _sessionId;
        }
        if (current is not null && !segments.Any(s => s.Id == current.Id))
            segments = [.. segments, current];
        if (segments.Length == 0) return null;

        var latest = current ?? segments.MaxBy(s => s.StartedAt)!;
        var first = segments.MinBy(s => s.StartedAt)!;
        long combatMs = segments.Sum(s => s.CombatMs);
        long clockMs = segments.Sum(s => s.ClockMs);
        double seconds = Math.Max(0.001, combatMs / 1000.0);
        long damage = segments.Sum(s => s.TotalDamage);
        var rows = segments.SelectMany(s => s.Combatants)
            .GroupBy(c => c.IsSelf ? "self" : c.Name.StartsWith('#')
                ? $"actor:{c.ActorId}" : $"name:{c.Name.ToUpperInvariant()}")
            .Select(group =>
            {
                var last = group.Last();
                long rowDamage = group.Sum(c => c.Damage);
                long healing = group.Sum(c => c.Healing ?? 0);
                int hits = group.Sum(c => c.Hits);
                return last with
                {
                    Class = group.Select(c => c.Class).LastOrDefault(c => c != GameClass.Unknown),
                    Damage = rowDamage,
                    Dps = rowDamage / seconds,
                    Share = damage > 0 ? (double)rowDamage / damage : 0,
                    Hits = hits,
                    CritRate = hits > 0 ? group.Sum(c => c.CritRate * c.Hits) / hits : 0,
                    MaxHit = group.Max(c => c.MaxHit),
                    DamageTaken = group.Sum(c => c.DamageTaken),
                    Healing = healing,
                    Hps = healing / seconds
                };
            })
            .OrderByDescending(c => c.Damage).ToArray();
        return latest with
        {
            Id = sessionId,
            StartedAt = first.StartedAt,
            ClockMs = clockMs,
            CombatMs = combatMs,
            IsActive = current?.IsActive == true,
            Boss = current is { IsActive: true } ? current.Boss : null,
            TotalDamage = damage,
            PartyDps = damage / seconds,
            Combatants = rows,
            Attackers = rows.Length
        };
    }
}
