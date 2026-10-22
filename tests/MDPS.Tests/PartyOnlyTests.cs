using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using MDPS;

public class PartyOnlyTests
{
    [Fact]
    public void New_party_roster_discards_old_observed_members()
    {
        var filter = new PartyOnly();
        filter.Update(new PartyRosterEvent(1000, [new PartyMemberInfo("Antigo", 2001, 1, 0, 0, GameClass.Ranger)]));
        filter.Observe(new PartyMemberObservedEvent(1100, 9, "Antigo", 2001));
        filter.Update(new PartyRosterEvent(2000, [new PartyMemberInfo("Novo", 2001, 1, 0, 0, GameClass.Cleric)]));
        Assert.Equal(["Novo"], filter.MissingMembers(null).Select(m => m.Name));
        Assert.DoesNotContain(filter.Filter(CrowdFight() with { Combatants =
            [.. CrowdFight().Combatants, new CombatantSnapshot(9, "Antigo", GameClass.Ranger, false, 1000, 1000, 0, 1, 0, 1000, 0)] })!.Combatants,
            c => c.Name == "Antigo");
    }
    [Fact]
    public void Party_actor_id_replaces_placeholder_with_profile_name_without_losing_damage()
    {
        var filter = new PartyOnly();
        filter.Observe(new PartyMemberObservedEvent(1000, 2, "Aliado", 2001));
        var fight = CrowdFight();
        var unnamed = fight with { Combatants = fight.Combatants.Select(c => c.ActorId == 2 ? c with { Name = "#2" } : c).ToArray() };
        var visible = filter.Filter(unnamed)!;
        Assert.Contains(visible.Combatants, c => c.ActorId == 2 && c.Name == "Aliado" && c.Damage > 0);
    }
    private static EncounterSnapshot CrowdFight() => new(Guid.NewGuid(), "Chefe", "Mapa", DateTimeOffset.UtcNow,
        10000, 10000, true, EncounterEndReason.None, 100000, 10000, null,
        [
            new CombatantSnapshot(1, "Eu", GameClass.Cleric, true, 10000, 1000, .1, 1, 0, 10000, 0, 2001),
            new CombatantSnapshot(2, "Aliado", GameClass.Ranger, false, 30000, 3000, .3, 1, 0, 30000, 0, 2001),
            new CombatantSnapshot(3, "Desconhecido", GameClass.Ranger, false, 60000, 6000, .6, 1, 0, 60000, 0, 2001)
        ], Attackers: 3);

    [Fact]
    public void WithoutRosterOnlyOwnDamageIsVisible()
    {
        var filter = new PartyOnly();
        var visible = filter.Filter(CrowdFight())!;
        Assert.Single(visible.Combatants);
        Assert.Equal(10000, visible.TotalDamage);
        Assert.Equal(1000, visible.PartyDps);
        Assert.Equal(1, visible.Combatants[0].Share);
    }

    [Fact]
    public void RosterKeepsMembersWithoutReceivedDamageVisible()
    {
        var filter = new PartyOnly();
        filter.Update(new PartyRosterEvent(1000,
        [new PartyMemberInfo("Eu", 2001, 1, 0, 0, GameClass.Cleric),
         new PartyMemberInfo("Aliado", 2001, 1, 0, 0, GameClass.Ranger),
         new PartyMemberInfo("Terceiro", 2001, 1, 0, 0, GameClass.Gladiator),
         new PartyMemberInfo("Quarto", 2001, 1, 0, 0, GameClass.Ranger),
         new PartyMemberInfo("Quinto", 2001, 1, 0, 0, GameClass.Cleric)]));
        var visible = filter.Filter(CrowdFight());
        Assert.Equal(["Terceiro", "Quarto", "Quinto"], filter.MissingMembers(visible).Select(m => m.Name));
        Assert.Equal(40000, visible!.TotalDamage);
    }

    [Fact]
    public void CurrentPartyStatusUsesKnownActorIdentityAndExcludesBystanders()
    {
        var filter = new PartyOnly();
        filter.Observe(new PartyMemberObservedEvent(1000, 2));
        filter.Observe(new PartyMemberObservedEvent(1000, 4));
        filter.ObserveKnownPlayers([new CachedPlayer(2, "Aliado", GameClass.Ranger, 2001),
            new CachedPlayer(4, "Sem dano", GameClass.Cleric, 2001)]);
        var visible = filter.Filter(CrowdFight())!;
        Assert.Equal(["Eu", "Aliado"], visible.Combatants.Select(c => c.Name));
        Assert.Equal(["Sem dano"], filter.MissingMembers(visible).Select(m => m.Name));
    }

    [Fact]
    public void FivePlayerDungeonCanFillUnannouncedMembersFromCorroboratedIdentities()
    {
        var filter = new PartyOnly();
        filter.Observe(new PartyMemberObservedEvent(1000, 2));
        filter.Observe(new PartyMemberObservedEvent(1000, 3));
        var players = Enumerable.Range(1, 5).Select(id =>
            new CachedPlayer((uint)id, "Player" + id, GameClass.Ranger, 2001)).ToArray();
        filter.InferDungeonRoster(new SessionState(1, 610062, players, []));
        Assert.True(filter.IsInferred);
        Assert.Equal(5, filter.MemberCount);
        filter.InferDungeonRoster(new SessionState(1, 1000, players, []));
        Assert.False(filter.IsInferred);
    }

    [Fact]
    public void FirstPartyLoadMergesPartialAndCompleteCopiesOfTheSamePlayer()
    {
        var filter = new PartyOnly();
        filter.Update(new PartyRosterEvent(1000,
        [new PartyMemberInfo("Trojão", 0, 0, 0, 0, GameClass.Unknown),
         new PartyMemberInfo("LoveBloodKil", 0, 0, 0, 0, GameClass.Unknown)]));
        filter.Observe(new PartyMemberObservedEvent(1100, 20, "Trojão", 2001));
        filter.Observe(new PartyMemberObservedEvent(1100, 21, "LoveBloodKil", 2001));
        filter.ObserveKnownPlayers(
        [new CachedPlayer(20, "Trojão", GameClass.Ranger, 2001),
         new CachedPlayer(21, "LoveBloodKil", GameClass.Cleric, 2001)]);

        var members = filter.MissingMembers(null);

        Assert.Equal(2, filter.MemberCount);
        Assert.Equal(["Trojão", "LoveBloodKil"], members.Select(m => m.Name));
        Assert.Equal([GameClass.Ranger, GameClass.Cleric], members.Select(m => m.Class));
        Assert.All(members, member => Assert.Equal(2001, member.ServerId));
    }

    [Fact]
    public void PartyDamageAndSharesExcludeBystandersAndFollowRosterChanges()
    {
        var filter = new PartyOnly();
        filter.Update(new PartyRosterEvent(1000, [new PartyMemberInfo("Aliado", 2001, 1, 0, 0, GameClass.Ranger)]));
        var visible = filter.Filter(CrowdFight())!;
        Assert.Equal(["Eu", "Aliado"], visible.Combatants.Select(c => c.Name));
        Assert.Equal(40000, visible.TotalDamage);
        Assert.Equal(4000, visible.PartyDps);
        Assert.Equal(.25, visible.Combatants[0].Share);
        Assert.Equal(.75, visible.Combatants[1].Share);
        filter.Update(new PartyRosterEvent(2000, []));
        Assert.Single(filter.Filter(CrowdFight())!.Combatants);
    }

    [Fact]
    public void PartyTimerStartsAtFirstVisiblePartyHitInsteadOfAnOutsidersPull()
    {
        var filter = new PartyOnly();
        filter.Update(new PartyRosterEvent(1000, [new PartyMemberInfo("Eu", 2001, 1, 0, 0, GameClass.Cleric)]));
        var fight = CrowdFight();
        fight = fight with
        {
            CombatMs = 76_000,
            Combatants =
            [
                fight.Combatants[0] with { Damage = 1_920_000, FirstHitMs = 17_000, LastHitMs = 77_000 },
                fight.Combatants[2] with { FirstHitMs = 1_000, LastHitMs = 77_000 }
            ]
        };

        var visible = filter.Filter(fight)!;

        Assert.Equal(60_000, visible.CombatMs);
        Assert.Equal(32_000, visible.PartyDps);
        Assert.Equal(32_000, Assert.Single(visible.Combatants).Dps);
    }

    [Fact]
    public void SameNameOnAnotherServerAndUnknownActorsStayHidden()
    {
        var filter = new PartyOnly();
        filter.Update(new PartyRosterEvent(1000, [new PartyMemberInfo("Aliado", 9000, 1, 0, 0, GameClass.Ranger),
            new PartyMemberInfo("#55", 2001, 1, 0, 0, GameClass.Ranger)]));
        var original = CrowdFight();
        var withUnknown = original with { Combatants = [.. original.Combatants,
            new CombatantSnapshot(55, "#55", GameClass.Unknown, false, 999, 99.9, 0, 1, 0, 999, 0, 2001)] };
        Assert.Equal(["Eu"], filter.Filter(withUnknown)!.Combatants.Select(c => c.Name));
    }

    [Fact]
    public void SavedRecordAndDetailsContainOnlyParty()
    {
        var filter = new PartyOnly();
        filter.Update(new PartyRosterEvent(1000, [new PartyMemberInfo("Aliado", 2001, 1, 0, 0, GameClass.Ranger)]));
        var fight = CrowdFight();
        var details = fight.Combatants.Select(c => new CombatantDetail(c.ActorId, c.Name, c.Class, c.IsSelf,
            c.Damage, c.Dps, c.Share, 10000, 0, 0, new HitQuality(0, 0, null, null, null, null, null, null, 0, null),
            [], [], [])).ToArray();
        var record = filter.Filter(new FightRecord(1, fight, details));
        Assert.Equal(2, record.Summary.Combatants.Count);
        Assert.Equal([1u, 2u], record.Details.Select(d => d.ActorId));
        Assert.DoesNotContain("Desconhecido", CsvReport.Create(record.Summary, false));
    }

    [Fact]
    public void SavedHistoryIncludesPartyMembersWhoSentNoDamage()
    {
        var filter = new PartyOnly();
        filter.Update(new PartyRosterEvent(1000,
        [new PartyMemberInfo("Eu", 2001, 1, 0, 0, GameClass.Cleric),
         new PartyMemberInfo("Aliado", 2001, 1, 0, 0, GameClass.Ranger),
         new PartyMemberInfo("Sem dano", 2001, 1, 0, 0, GameClass.Sorcerer)]));

        var saved = filter.IncludeRoster(filter.Filter(new FightRecord(1, CrowdFight(), [])));

        Assert.Equal(["Eu", "Aliado", "Sem dano"], saved.Summary.Combatants.Select(c => c.Name));
        var missing = saved.Summary.Combatants.Single(c => c.Name == "Sem dano");
        Assert.Equal(GameClass.Sorcerer, missing.Class);
        Assert.Equal(0, missing.Damage);
    }
}
