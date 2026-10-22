using AionMeter.Core.Events;
using AionMeter.Core.Game;

namespace MDPS;

/// <summary>Recognizes a fresh run of the same dungeon from its server countdown.</summary>
public sealed class DungeonInstanceSession
{
    private readonly Dictionary<(int MapId, int Kind), (long StartMs, long EndMs)> _timers = [];

    public bool Observe(InstanceTimerEvent timer)
    {
        if (!GameData.IsDungeonMap(timer.MapId) || timer.Kind == InstanceTimerEvent.BossLimit)
            return false;

        var key = (timer.MapId, timer.Kind);
        var value = (timer.StartMs, timer.EndMs);
        var isNewRun = _timers.TryGetValue(key, out var previous) && previous != value;
        _timers[key] = value;
        return isNewRun;
    }
}
