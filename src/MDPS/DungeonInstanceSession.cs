using AionMeter.Core.Events;
using AionMeter.Core.Game;

namespace MDPS;

/// <summary>Recognizes a fresh run of the same dungeon from its server countdown.</summary>
public sealed class DungeonInstanceSession
{
    private readonly Dictionary<(int MapId, int Kind), (long StartMs, long EndMs)> _timers = [];

    public bool Observe(InstanceTimerEvent timer)
    {
        // Ticket reentries can report a server map id that is not in the local dungeon table.
        // The instance countdown itself is the reliable signal; boss-limit timers are excluded.
        if (timer.Kind == InstanceTimerEvent.BossLimit)
            return false;

        var key = (timer.MapId, timer.Kind);
        var value = (timer.StartMs, timer.EndMs);
        var isNewRun = _timers.TryGetValue(key, out var previous) && previous != value;
        _timers[key] = value;
        return isNewRun;
    }
}
