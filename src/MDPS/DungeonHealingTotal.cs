using AionMeter.Core.Events;

namespace MDPS;

/// <summary>Healing by the local character during the current dungeon session.</summary>
public sealed class DungeonHealingTotal
{
    private readonly object _gate = new();
    private long _total;

    public void NewSession()
    {
        lock (_gate) _total = 0;
    }

    public void ObserveHeal(HealEvent heal, uint? selfId)
    {
        if (selfId is null || heal.SourceId != selfId || heal.Amount <= 0) return;
        lock (_gate) _total += heal.Amount;
    }

    public long Total()
    {
        lock (_gate) return _total;
    }
}
