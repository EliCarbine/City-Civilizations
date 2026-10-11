// Citizen Soul -- CivicSystem (M5h): the block and city hall (F100-F108).
// Volunteering and its fatigue; the watch, its walks and its overreach;
// garden plots, tomato feuds and vandalism; petitions, marches and answers;
// town halls, rousing speeches and first timers; giving and beaten goals;
// jury duty and the trial a soul never stops retelling; block parties and
// cleanups; and the crisis that either scars a street or welds it together.
// Reads personality, ticks and edges; writes only its own surfaces.
using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F100-F108: community & civic life -- volunteering, watch, gardens,
    /// petitions, town halls, giving, jury duty, block life, crisis.
    /// </summary>
    public partial class CivicSystem : SoulSystemBase
    {
        private const int CivicLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        /// <summary>128 lanes -> 1 pass/citizen/day.</summary>
        protected override int Lanes { get { return CivicLanes; } }

        private EntityQuery m_Civics;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Civics = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulCivic>());
            m_All = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            EntityManager em = EntityManager;

            try
            {
                if (m_All.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> all = m_All.ToEntityArray(Allocator.Temp);
                    int filled = 0;
                    for (int i = 0; i < all.Length && filled < MigrateBudget; i++)
                    {
                        Entity e = all[i];
                        if (em.HasComponent<SoulCivic>(e)) continue;
                        try
                        {
                            em.AddComponentData(e, SoulCivic.Fresh(day));
                            filled++;
                        }
                        catch (Exception ex)
                        {
                            SoulLog.Exception("civic.migrate." + e.Index, ex);
                        }
                    }
                    all.Dispose();
                }
            }
            catch (Exception ex)
            {
                SoulLog.Once("civic.migrate", "CivicSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                NativeArray<Entity> souls = m_Civics.ToEntityArray(Allocator.Temp);
                try
                {
                    int faults = 0;
                    for (int i = 0; i < souls.Length; i++)
                    {
                        Entity e = souls[i];
                        if (!Matches(e, lane, CivicLanes)) continue;
                        try
                        {
                            Advance(em, e, day);
                        }
                        catch (Exception ex)
                        {
                            faults++;
                            if (faults <= FaultLogCap) SoulLog.Exception("civic.fault." + e.Index, ex);
                        }
                    }
                }
                finally
                {
                    souls.Dispose();
                }
            }
            catch (Exception ex)
            {
                SoulLog.Exception("civic.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulCivic>(e)) return;
            SoulCivic c = em.GetComponentData<SoulCivic>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            uint age = day >= meta.m_BirthDay ? day - meta.m_BirthDay : 0u;
            uint salt = (uint)e.Index * 2654435761u;

            byte agreeableness = 50;
            byte extraversion = 50;
            bool hasPersonality = em.HasComponent<SoulPersonality>(e);
            if (hasPersonality)
            {
                SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
                agreeableness = p.m_Agreeableness;
                extraversion = p.m_Extraversion;
            }

            // A. volunteering (F100): the warm ones sign up; some find their
            //    regulars; some burn out on giving.
            if (age >= (uint)SoulTune.CivicMinAgeDays && (c.m_Flags & SoulCivicFlags.Volunteer) == 0 &&
                (agreeableness >= SoulTune.CivicWarmAgreeMin || Roll(day, salt + 3u, 100u) < 30u) &&
                (day + salt) % 90u == 0u && Roll(day, salt + 5u, 1000u) < (uint)SoulTune.CivicVolFormPerMille)
            {
                c.m_Flags |= SoulCivicFlags.Volunteer;
                c.m_Cause = (byte)Roll(day, salt + 7u, 3u);   // kitchen / shelter / animals
                SoulFeel.Note(em, e, day, SoulEventKind.Volunteered, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.CivicVolMood);
            }
            if ((c.m_Flags & SoulCivicFlags.Volunteer) != 0 && (day + salt) % (uint)SoulTune.CivicVolRecurDays == 0u)
            {
                if (Roll(day, salt + 11u, 1000u) < (uint)SoulTune.CivicFatiguePerMille)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.VolFatigue, (short)c.m_Acts, e);
                    SoulFeel.Pulse(em, e, SoulTune.CivicFatigueMood);
                }
                else
                {
                    if (c.m_Acts < 60000) c.m_Acts++;
                    SoulFeel.Note(em, e, day, SoulEventKind.Volunteered, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.CivicVolMood);
                    if (c.m_Acts >= SoulTune.CivicCliqueActs && c.m_Mate == Entity.Null)
                    {
                        Entity mate = FindFriend(em, e, SoulTune.CurioPartnerMinAffinity);
                        if (mate != Entity.Null)
                        {
                            c.m_Mate = mate;
                            BumpBoth(em, e, mate, SoulTune.CivicCliqueBump);
                            SoulFeel.Note(em, e, day, SoulEventKind.VolClique, 1, mate);
                            SoulFeel.Pulse(em, e, SoulTune.CivicVolMood);
                        }
                    }
                }
            }

            // B. the watch (F101): the walks build trust; one night goes too far.
            if (age >= (uint)SoulTune.CivicMinAgeDays && (c.m_Flags & SoulCivicFlags.Watch) == 0 &&
                (day + salt) % 120u == 0u && Roll(day, salt + 17u, 1000u) < (uint)SoulTune.CivicWatchFormPerMille)
            {
                c.m_Flags |= SoulCivicFlags.Watch;
                if (c.m_Mate == Entity.Null)
                {
                    Entity mate = FindFriend(em, e, SoulTune.CurioPartnerMinAffinity);
                    if (mate != Entity.Null) c.m_Mate = mate;
                }
                SoulFeel.Note(em, e, day, SoulEventKind.WatchFormed, 1, c.m_Mate);
                SoulFeel.Pulse(em, e, SoulTune.CivicWatchMood);
            }
            if ((c.m_Flags & SoulCivicFlags.Watch) != 0 && (day + salt) % (uint)SoulTune.CivicWatchRecurDays == 0u)
            {
                uint pick = Roll(day, salt + 19u, 100u);
                if (pick < 60u)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.PatrolWalk, 1, c.m_Mate);
                    SoulFeel.Pulse(em, e, SoulTune.CivicWatchMood);
                    if (c.m_Mate != Entity.Null && em.Exists(c.m_Mate))
                    {
                        BumpBoth(em, e, c.m_Mate, SoulTune.CivicWatchTrustBump);
                    }
                }
                else if (pick < (uint)(60 + SoulTune.CivicOverzealPerMille / 10))
                {
                    c.m_Flags |= SoulCivicFlags.WatchOver;
                    if (c.m_Mate != Entity.Null && em.Exists(c.m_Mate))
                    {
                        BumpBoth(em, e, c.m_Mate, SoulTune.CivicOverzealBump);
                    }
                    SoulFeel.Note(em, e, day, SoulEventKind.WatchOverreach, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.CivicOverzealMood);
                }
            }

            // C. the garden (F102): a plot, a feud over tomatoes, a harvest
            //    shared -- and one morning, wreckage.
            if (age >= (uint)SoulTune.CivicMinAgeDays && (c.m_Flags & SoulCivicFlags.Gardener) == 0 &&
                (day + salt) % 150u == 0u && Roll(day, salt + 23u, 1000u) < (uint)SoulTune.CivicGardenFormPerMille)
            {
                c.m_Flags |= SoulCivicFlags.Gardener;
                if (c.m_Mate == Entity.Null)
                {
                    Entity mate = FindFriend(em, e, SoulTune.CurioPartnerMinAffinity);
                    if (mate != Entity.Null) c.m_Mate = mate;
                }
                SoulFeel.Note(em, e, day, SoulEventKind.GardenPlot, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.CivicGardenMood);
            }
            if ((c.m_Flags & SoulCivicFlags.Gardener) != 0 && (day + salt) % 90u == 0u)
            {
                uint pick = Roll(day, salt + 29u, 1000u);
                if (pick < (uint)SoulTune.CivicTomatoPerMille)
                {
                    if (c.m_Mate != Entity.Null && em.Exists(c.m_Mate))
                    {
                        BumpBoth(em, e, c.m_Mate, SoulTune.CivicTomatoBump);
                        SoulFeel.Note(em, e, day, SoulEventKind.TomatoRival, 1, c.m_Mate);
                        SoulFeel.Note(em, c.m_Mate, day, SoulEventKind.TomatoRival, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.CivicTomatoMood);
                    }
                }
                else if (pick < 550u)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.HarvestShare, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.CivicHarvestMood);
                    if (c.m_Mate != Entity.Null && em.Exists(c.m_Mate))
                    {
                        BumpBoth(em, e, c.m_Mate, SoulTune.CivicHarvestBump);
                    }
                }
            }
            if ((c.m_Flags & SoulCivicFlags.Gardener) != 0 && day > 0u && day % 400u == 0u &&
                Roll(day, salt + 31u, 1000u) < (uint)SoulTune.CivicVandalPerMille)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.GardenVandal, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.CivicVandalMood);
            }

            // D. petitions & the march (F103): a cause, a crowd, an answer.
            if (age >= (uint)SoulTune.CivicMinAgeDays && (c.m_Flags & SoulCivicFlags.Petitioner) == 0 &&
                (day + salt) % 200u == 0u && Roll(day, salt + 37u, 1000u) < (uint)SoulTune.CivicPetitionFormPerMille)
            {
                c.m_Flags |= SoulCivicFlags.Petitioner;
                c.m_Cause = (byte)Roll(day, salt + 41u, 5u);   // corner / school / garden / trees / road
                c.m_PetitionDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.Petition, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.CivicPetitionMood);
            }
            if ((c.m_Flags & SoulCivicFlags.Petitioner) != 0 && c.m_PetitionDay > 0u &&
                day >= c.m_PetitionDay + (uint)SoulTune.CivicPetitionRunDays)
            {
                if (Roll(day, salt + 43u, 1000u) < (uint)SoulTune.CivicWinPerMille)
                {
                    if (c.m_Repute < 250) c.m_Repute += 10;
                    SoulFeel.Note(em, e, day, SoulEventKind.PetitionWin, (short)c.m_Repute, e);
                    SoulFeel.Pulse(em, e, SoulTune.CivicWinMood);
                }
                else
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.PetitionIgnored, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.CivicIgnoredMood);
                }
                c.m_PetitionDay = 0u;
                c.m_Flags = (byte)(c.m_Flags & ~SoulCivicFlags.Petitioner);
            }
            if (day > 0u && day % 365u == 77u && Roll(day, salt + 47u, 100u) < 40u)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.March, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.CivicMarchMood);
            }

            // E. town halls (F104): first timers get the bug; the regulars
            //    take the floor.
            if (age >= (uint)SoulTune.CivicMinAgeDays && (day + salt) % (uint)SoulTune.CivicTownRecurDays == 0u &&
                Roll(day, salt + 53u, 1000u) < (uint)SoulTune.CivicTownFormPerMille)
            {
                if ((c.m_Flags & SoulCivicFlags.TownRegular) == 0)
                {
                    c.m_Flags |= SoulCivicFlags.TownRegular;
                    SoulFeel.Note(em, e, day, SoulEventKind.FirstTimer, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.CivicFirstTimerMood);
                }
                else
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.TownHall, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.CivicTownMood);
                    if (c.m_Clout < 250) c.m_Clout = (byte)(c.m_Clout + 5);
                    if (extraversion >= SoulTune.CivicBoldExtraMin && c.m_Clout >= SoulTune.CivicSpeechCloutMin &&
                        Roll(day, salt + 59u, 1000u) < (uint)SoulTune.CivicSpeechPerMille)
                    {
                        if (c.m_Clout < 250) c.m_Clout = (byte)(c.m_Clout + 10);
                        SoulFeel.Note(em, e, day, SoulEventKind.Speech, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.CivicSpeechMood);
                    }
                }
            }

            // F. the drive (F105): coins into tins, and sometimes the whole
            //    district clears the goal.
            if (age >= (uint)SoulTune.CivicMinAgeDays && (day + salt) % 180u == 0u &&
                Roll(day, salt + 61u, 1000u) < (uint)SoulTune.CivicGivePerMille)
            {
                c.m_Flags |= SoulCivicFlags.Charitable;
                SoulFeel.Note(em, e, day, SoulEventKind.GiveDrive, (short)c.m_Acts, e);
                SoulFeel.Pulse(em, e, SoulTune.CivicGiveMood);
                if (c.m_Acts >= SoulTune.CivicGiveGoalActs && Roll(day, salt + 67u, 100u) < 25u)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.GoalBeaten, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.CivicGoalMood);
                }
            }

            // G. jury duty (F106): the summons, the foreman's chair, and the
            //    story told again years later.
            if (age >= (uint)SoulTune.CivicMinAgeDays && (c.m_Flags2 & SoulCivicFlags2.JuryServed) == 0 &&
                (day + salt) % 500u == 0u && Roll(day, salt + 71u, 1000u) < (uint)SoulTune.CivicJuryPerMille)
            {
                c.m_Flags2 |= SoulCivicFlags2.JuryServed;
                SoulFeel.Note(em, e, day, SoulEventKind.JurySummons, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.CivicJuryMood);
                if (Roll(day, salt + 73u, 1000u) < (uint)SoulTune.CivicForemanPerMille)
                {
                    c.m_Flags2 |= SoulCivicFlags2.Foreman;
                    if (c.m_Repute < 250) c.m_Repute += 5;
                    SoulFeel.Note(em, e, day, SoulEventKind.Foreman, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.CivicForemanMood);
                }
            }
            if ((c.m_Flags2 & SoulCivicFlags2.JuryServed) != 0 && (day + salt) % 365u == 0u &&
                extraversion >= SoulTune.CivicBoldExtraMin && Roll(day, salt + 79u, 100u) < 30u)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.JuryStory, (short)c.m_Repute, e);
                SoulFeel.Pulse(em, e, SoulTune.CivicJuryStoryMood);
            }

            // H. block life (F107): the party every year, the spring cleanup.
            if (day > 0u && day % 365u == 201u && Roll(day, salt + 83u, 100u) < 50u)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.BlockParty, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.CivicPartyMood);
                if (c.m_Mate != Entity.Null && em.Exists(c.m_Mate))
                {
                    BumpBoth(em, e, c.m_Mate, SoulTune.CivicHarvestBump);
                }
            }
            if (day > 0u && day % 365u == 90u && Roll(day, salt + 89u, 100u) < 60u)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.Cleanup, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.CivicCleanupMood);
                if (c.m_Mate != Entity.Null && em.Exists(c.m_Mate))
                {
                    BumpBoth(em, e, c.m_Mate, SoulTune.CivicCleanupBump);
                }
            }

            // I. the crisis (F108): helpers, heroes, shared grief -- and the
            //    ones who still hear that night.
            if (day > 0u && day % 400u == 0u)
            {
                uint roll = Roll(day, salt + 97u, 1000u);
                if (roll < (uint)SoulTune.CivicCrisisPerMille)
                {
                    uint kind = Roll(day, salt + 101u, 100u);
                    if (kind < 40u)
                    {
                        c.m_Flags |= SoulCivicFlags.CrisisHelper;
                        SoulFeel.Note(em, e, day, SoulEventKind.CrisisHelp, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.CivicCrisisHelpMood);
                    }
                    else if (kind < 55u)
                    {
                        if (c.m_Repute < 245) c.m_Repute = (byte)(c.m_Repute + 15);
                        SoulFeel.Note(em, e, day, SoulEventKind.CrisisHero, (short)c.m_Repute, e);
                        SoulFeel.Pulse(em, e, SoulTune.CivicCrisisHeroMood);
                    }
                    else if (kind < 85u)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.CrisisCohesion, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.CivicCrisisHelpMood);
                        if (c.m_Mate != Entity.Null && em.Exists(c.m_Mate))
                        {
                            BumpBoth(em, e, c.m_Mate, SoulTune.CivicCrisisCohesionBump);
                        }
                    }
                    else
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.CrisisTrauma, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.CivicCrisisTraumaMood);
                    }
                }
            }

            em.SetComponentData(e, c);
        }

        // ---- helpers --------------------------------------------------------
        private static Entity FindFriend(EntityManager em, Entity e, int minAffinity)
        {
            if (!em.HasBuffer<SoulEdge>(e)) return Entity.Null;
            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
            for (int i = 0; i < edges.Length; i++)
            {
                SoulEdge ed = edges[i];
                Entity o = ed.m_Other;
                if (o == e) continue;
                if (!em.Exists(o)) continue;
                if (ed.m_Affinity < minAffinity) continue;
                return o;
            }
            return Entity.Null;
        }

        private static void BumpBoth(EntityManager em, Entity a, Entity b, int delta)
        {
            if (a == Entity.Null || b == Entity.Null) return;
            BumpEdge(em, a, b, delta);
            BumpEdge(em, b, a, delta);
        }

        private static void BumpEdge(EntityManager em, Entity from, Entity to, int delta)
        {
            if (!em.HasBuffer<SoulEdge>(from)) return;
            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(from);
            for (int i = 0; i < edges.Length; i++)
            {
                SoulEdge ed = edges[i];
                if (ed.m_Other != to) continue;
                int next = ed.m_Affinity + delta;
                if (next < 0) next = 0;
                if (next > 100) next = 100;
                ed.m_Affinity = (sbyte)next;
                edges[i] = ed;
                return;
            }
        }

        private static uint Roll(uint day, uint salt, uint mod)
        {
            if (mod == 0u) return 0u;
            uint h = day * 747796405u + salt + 2246822519u;
            h ^= h >> 15;
            h *= 2654435761u;
            h ^= h >> 13;
            return h % mod;
        }
    }
}
