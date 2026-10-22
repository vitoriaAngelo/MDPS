using AionMeter.Core.Events;
using AionMeter.Core.Game;

namespace MDPS;

public static class DungeonEntryReset
{
    public static bool ShouldReset(int previousMapId, ZoneChangedEvent zone) =>
        !zone.IsTeleport && zone.MapId != 0 && zone.MapId != previousMapId &&
        GameData.IsDungeonMap(previousMapId) != (zone.IsDungeon || GameData.IsDungeonMap(zone.MapId));
}
