using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;

namespace MDPS;

/// <summary>Projects a combat snapshot onto the party roster sent by the game.</summary>
public sealed class PartyOnly
{
    private readonly object _gate = new();
    private PartyMemberInfo[]? _members;
    private readonly HashSet<uint> _actorIds = [];
    private readonly Dictionary<uint, PartyMemberInfo> _namedActors = [];
    private readonly Dictionary<uint, CachedPlayer> _knownPlayers = [];
    private readonly Dictionary<string, PartyMemberInfo> _profileMembers = new(StringComparer.OrdinalIgnoreCase);
    private PartyMemberInfo[] _inferredMembers = [];

    public bool HasRoster { get { lock (_gate) return _members is not null || _actorIds.Count > 0 || _profileMembers.Count > 0; } }
    public int MemberCount { get { lock (_gate) return AllMembers().Length; } }
    public bool IsInferred { get { lock (_gate) return _inferredMembers.Length > 0; } }

    public void InferDungeonRoster(SessionState identity)
    {
        lock (_gate)
        {
            _inferredMembers = [];
            if (!GameData.IsDungeonMap(identity.MapId) || _members is not null || _profileMembers.Count >= 5 ||
                identity.Players.Count != 5 || identity.SelfId is not { } selfId ||
                !identity.Players.Any(p => p.Id == selfId) ||
                identity.Players.Count(p => _actorIds.Contains(p.Id) && p.Id != selfId) < 2) return;
            _inferredMembers = identity.Players.Select(p => new PartyMemberInfo(p.Name, p.ServerId, 0, 0, 0, p.Class)).ToArray();
        }
    }

    public void Observe(PartyMemberObservedEvent member)
    {
        lock (_gate)
        {
            if (member.ActorId != 0) _actorIds.Add(member.ActorId);
            if (!string.IsNullOrWhiteSpace(member.Name))
            {
                _profileMembers[member.Name] = new PartyMemberInfo(member.Name, member.ServerId, 0, 0, 0, GameClass.Unknown);
                if (member.ActorId != 0) _namedActors[member.ActorId] = _profileMembers[member.Name];
            }
        }
    }

    public void ObserveKnownPlayers(IEnumerable<CachedPlayer> players)
    {
        lock (_gate)
            foreach (var player in players)
                if (_actorIds.Contains(player.Id)) _knownPlayers[player.Id] = player;
    }

    private PartyMemberInfo[] AllMembers()
    {
        var members = (_members ?? []).Concat(_profileMembers.Values).Concat(_inferredMembers)
            .Concat(_knownPlayers.Values.Select(p => new PartyMemberInfo(p.Name, p.ServerId, 0, 0, 0, p.Class)));
        var merged = new List<PartyMemberInfo>();
        foreach (var member in members.Where(m => !string.IsNullOrWhiteSpace(m.Name)))
        {
            int index = merged.FindIndex(existing =>
                string.Equals(existing.Name, member.Name, StringComparison.OrdinalIgnoreCase)
                && (existing.ServerId == 0 || member.ServerId == 0 || existing.ServerId == member.ServerId));
            if (index < 0)
            {
                merged.Add(member);
                continue;
            }

            var existing = merged[index];
            merged[index] = existing with
            {
                ServerId = existing.ServerId != 0 ? existing.ServerId : member.ServerId,
                Level = Math.Max(existing.Level, member.Level),
                GearScore = Math.Max(existing.GearScore, member.GearScore),
                CombatPower = Math.Max(existing.CombatPower, member.CombatPower),
                Class = existing.Class != GameClass.Unknown ? existing.Class : member.Class
            };
        }
        return merged.ToArray();
    }

    public PartyMemberInfo[] MissingMembers(EncounterSnapshot? snapshot)
    {
        PartyMemberInfo[]? roster;
        lock (_gate) roster = AllMembers();
        return roster.Where(m => snapshot is null || !snapshot.Combatants.Any(c =>
            string.Equals(c.Name, m.Name, StringComparison.OrdinalIgnoreCase)
            && (m.ServerId == 0 || c.ServerId == 0 || m.ServerId == c.ServerId)))
            .ToArray();
    }

    public void Reset()
    {
        lock (_gate) { _members = null; _actorIds.Clear(); _namedActors.Clear(); _knownPlayers.Clear(); _profileMembers.Clear(); _inferredMembers = []; }
    }

    public void Update(PartyRosterEvent roster)
    {
        lock (_gate)
        {
            var incoming = roster.Members.ToArray();
            if (_members is not null && _members.Any(old => !incoming.Any(next =>
                string.Equals(old.Name, next.Name, StringComparison.OrdinalIgnoreCase) &&
                (old.ServerId == 0 || next.ServerId == 0 || old.ServerId == next.ServerId))))
            {
                _actorIds.Clear();
                _namedActors.Clear();
                _knownPlayers.Clear();
                _profileMembers.Clear();
                _inferredMembers = [];
            }
            _members = incoming;
        }
    }

    public EncounterSnapshot? Filter(EncounterSnapshot? snapshot)
    {
        if (snapshot is null) return null;
        PartyMemberInfo[]? roster;
        HashSet<uint> actorIds;
        Dictionary<uint, PartyMemberInfo> namedActors;
        lock (_gate) { roster = AllMembers(); actorIds = new HashSet<uint>(_actorIds); namedActors = new(_namedActors); }
        var members = snapshot.Combatants.Where(c => actorIds.Contains(c.ActorId) || IsMember(c, roster))
            .Select(c => c.Name.StartsWith('#') && namedActors.TryGetValue(c.ActorId, out var member)
                ? c with { Name = member.Name, ServerId = member.ServerId, Class = member.Class == GameClass.Unknown ? c.Class : member.Class }
                : c).ToArray();
        long damage = members.Sum(c => c.Damage);
        var timed = members.Where(c => c.Damage > 0 && c.FirstHitMs > 0 && c.LastHitMs >= c.FirstHitMs).ToArray();
        long combatMs = timed.Length > 0
            ? Math.Max(1_000, timed.Max(c => c.LastHitMs) - timed.Min(c => c.FirstHitMs))
            : snapshot.CombatMs;
        double seconds = Math.Max(0.001, combatMs / 1000.0);
        var rows = members.Select(c => c with
        {
            Dps = c.Damage / seconds,
            Hps = c.Healing / seconds,
            Share = damage > 0 ? (double)c.Damage / damage : 0
        }).ToArray();
        return snapshot with
        {
            Combatants = rows,
            CombatMs = combatMs,
            TotalDamage = damage,
            PartyDps = damage / seconds,
            Attackers = rows.Length
        };
    }

    public FightRecord Filter(FightRecord record)
    {
        var summary = Filter(record.Summary)!;
        var ids = summary.Combatants.Select(c => c.ActorId).ToHashSet();
        return record with { Summary = summary, Details = record.Details.Where(d => ids.Contains(d.ActorId)).ToArray() };
    }

    /// <summary>Adds zero-damage rows for identified party members, so saved history keeps the whole party.</summary>
    public FightRecord IncludeRoster(FightRecord record)
    {
        var missing = MissingMembers(record.Summary);
        if (missing.Length == 0) return record;
        var rows = record.Summary.Combatants.ToList();
        rows.AddRange(missing.Select(member => new CombatantSnapshot(
            0, member.Name, member.Class, false, 0, 0, 0, 0, 0, 0, 0, member.ServerId)));
        return record with { Summary = record.Summary with { Combatants = rows } };
    }

    private static bool IsMember(CombatantSnapshot c, PartyMemberInfo[]? roster)
    {
        if (c.IsSelf) return true;
        if (c.IsUnknownSummons || roster is null || c.Name.StartsWith('#')) return false;
        return roster.Any(m => string.Equals(m.Name, c.Name, StringComparison.OrdinalIgnoreCase)
            && (m.ServerId == 0 || c.ServerId == 0 || m.ServerId == c.ServerId));
    }
}
