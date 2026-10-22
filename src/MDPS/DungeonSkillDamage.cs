using AionMeter.Core.Events;
using AionMeter.Core.Game;

namespace MDPS;

public sealed class DungeonSkillDamage
{
    private readonly Dictionary<int, (long Damage, int Hits)> _skills = [];

    public void Observe(DamageEvent hit, uint? selfId, int mapId, GameData data)
    {
        if (selfId is null || hit.SourceId != selfId || !GameData.IsDungeonMap(mapId) ||
            data.IsNonDamageSkill(hit.SkillCode) || data.HealSkills.Contains(hit.SkillCode) ||
            data.HealSkills.Contains(GameData.BaseSkill(hit.SkillCode))) return;
        var old = _skills.GetValueOrDefault(hit.SkillCode);
        _skills[hit.SkillCode] = (old.Damage + hit.Damage, old.Hits + Math.Max(1, hit.HitCount));
    }

    public IReadOnlyList<DungeonSkillTotal> Snapshot(GameData data)
    {
        var total = _skills.Values.Sum(x => x.Damage);
        return _skills.Select(x => new DungeonSkillTotal(x.Key, data.SkillName(x.Key), x.Value.Damage,
                x.Value.Hits, total > 0 ? x.Value.Damage / (double)total : 0))
            .OrderByDescending(x => x.Damage).ToList();
    }

    public void NewSession() => _skills.Clear();
}

public sealed record DungeonSkillTotal(int SkillCode, string Name, long Damage, int Hits, double Share);
