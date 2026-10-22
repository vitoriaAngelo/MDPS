using AionMeter.Core.Events;
using AionMeter.Core.Game;
using MDPS;

public class DungeonSkillDamageTests
{
    [Fact]
    public void Accumulates_only_the_local_players_damage_inside_the_dungeon()
    {
        var total = new DungeonSkillDamage();
        var data = new GameData();
        total.Observe(new DamageEvent(1, 7, 9, 16040000, 1_000, HitFlags.None, 2), 7, 600092, data);
        total.Observe(new DamageEvent(2, 8, 9, 16040000, 8_000, HitFlags.None), 7, 600092, data);
        total.Observe(new DamageEvent(3, 7, 9, 16050000, 8_000, HitFlags.None), 7, 110002, data);

        var skill = Assert.Single(total.Snapshot(data));
        Assert.Equal(1_000, skill.Damage);
        Assert.Equal(2, skill.Hits);
        Assert.Equal(1, skill.Share);
    }
}
