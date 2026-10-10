// Citizen Soul -- SportsSystem (M5e): the sporting life (F72-F81).
// Pickup runs and winner-stays crews; district leagues and season titles;
// running clubs and the city marathon; dojos, LAN nights, park chess and
// school teams; the derby days, the knocks, the comebacks, the last season.
// Reads quirks, personality and edges; writes only its own surfaces.
using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F72-F81: sports & competition -- pickup games, amateur leagues,
    /// running, martial arts, esports, chess, kids' leagues, championships,
    /// rivalries, and the injuries and comebacks in between.
    /// </summary>
    public partial class SportsSystem : SoulSystemBase
    {
        private const int SportLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        /// <summary>128 lanes -> 1 pass/citizen/day.</summary>
        protected override int Lanes { get { return SportLanes; } }

        private EntityQuery m_Sports;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Sports = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulSport>());
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
                // Migration: every soul carries a sport book.
                if (m_All.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> all = m_All.ToEntityArray(Allocator.Temp);
                    int filled = 0;
                    for (int i = 0; i < all.Length && filled < MigrateBudget; i++)
                    {
                        Entity e = all[i];
                        if (em.HasComponent<SoulSport>(e)) continue;
                        try
                        {
                            em.AddComponentData(e, SoulSport.Fresh(day));
                            filled++;
                        }
                        catch (Exception ex)
                        {
                            SoulLog.Exception("sport.migrate." + e.Index, ex);
                        }
                    }
                    all.Dispose();
                }
            }
            catch (Exception ex)
            {
                SoulLog.Once("sport.migrate", "SportsSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                NativeArray<Entity> souls = m_Sports.ToEntityArray(Allocator.Temp);
                try
                {
                    int faults = 0;
                    for (int i = 0; i < souls.Length; i++)
                    {
                        Entity e = souls[i];
                        if (!Matches(e, lane, SportLanes)) continue;
                        try
                        {
                            Advance(em, e, day);
                        }
                        catch (Exception ex)
                        {
                            faults++;
                            if (faults <= FaultLogCap) SoulLog.Exception("sport.fault." + e.Index, ex);
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
                SoulLog.Exception("sport.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulSport>(e)) return;
            SoulSport s = em.GetComponentData<SoulSport>(e);
            if (s.m_LastDay == day) return;
            s.m_LastDay = day;

            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            uint age = day >= meta.m_BirthDay ? day - meta.m_BirthDay : 0u;
            uint salt = (uint)e.Index * 2654435761u;

            byte conscientiousness = 100;
            bool fitness = false;
            bool hothead = false;
            bool loner = false;
            if (em.HasComponent<SoulPersonality>(e))
            {
                SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
                conscientiousness = p.m_Conscientiousness;
                fitness = SoulQuirks.Has(p.m_Quirk, SoulQuirks.Fitness);
                hothead = SoulQuirks.Has(p.m_Quirk, SoulQuirks.Hothead);
                loner = SoulQuirks.Has(p.m_Quirk, SoulQuirks.Loner);
            }

            // A. the knocks (F81): a sidelined soul mends, then comes back.
            if ((s.m_Flags & SoulSportFlags.Sidelined) != 0)
            {
                uint heal = (uint)(SoulTune.SportHealPerMille + conscientiousness / 10);
                if (Roll(day, salt + 3u, 1000u) < heal)
                {
                    s.m_Flags = (byte)(s.m_Flags & ~SoulSportFlags.Sidelined);
                    SoulFeel.Note(em, e, day, SoulEventKind.Comeback, (short)s.m_Skill, e);
                    SoulFeel.Pulse(em, e, SoulTune.SportComebackMood);
                }
                em.SetComponentData(e, s);
                return;
            }

            // B. the call of the field (F72-F78): souls find their sport.
            if (s.m_Sport == SoulSportKind.None && age >= (uint)SoulTune.SportMinAgeDays &&
                age < (uint)SoulTune.SportRetireAgeDays && (s.m_Flags & SoulSportFlags.Quit) == 0)
            {
                uint call = (uint)(SoulTune.SportPickPerMille + (fitness ? SoulTune.SportFitnessPickBonus : 0) + conscientiousness / 10);
                if (Roll(day, salt + 7u, 1000u) < call)
                {
                    byte pick = PickSport(day, salt, fitness, hothead, loner);
                    s.m_Sport = pick;
                    if (age < (uint)SoulTune.SportAdultDays)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.KidSportStart, pick, e);
                        SoulFeel.Pulse(em, e, SoulTune.SportKidStartMood);
                    }
                    else if (pick == SoulSportKind.Running)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.RunStarted, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.SportPracticeMood);
                    }
                    else if (pick == SoulSportKind.MartialArts)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.DojoJoined, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.SportPracticeMood);
                    }
                    else if (pick == SoulSportKind.Chess)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.ParkMatch, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.SportParkMood);
                    }
                    else
                    {
                        SoulFeel.Pulse(em, e, SoulTune.SportPracticeMood);
                    }
                    if (pick == SoulSportKind.Running && (s.m_Flags & SoulSportFlags.Club) == 0 &&
                        Roll(day, salt + 9u, 1000u) < (uint)SoulTune.SportRunClubPerMille)
                    {
                        s.m_Flags |= SoulSportFlags.Club;
                        SoulFeel.Note(em, e, day, SoulEventKind.CrewFormed, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.SportCrewMood);
                    }
                }
            }

            // C. practice (F74/F75): quiet hours on the court, the road, the mat.
            if (s.m_Sport != SoulSportKind.None)
            {
                uint train = (uint)(SoulTune.SportPracticePerMille + (fitness ? SoulTune.SportFitnessPickBonus : 0) + conscientiousness / 5);
                if (Roll(day, salt + 11u, 1000u) < train)
                {
                    if (s.m_Skill < 100u)
                    {
                        s.m_Skill += (uint)SoulTune.SportSkillGrow;
                        if (s.m_Skill > 100u) s.m_Skill = 100u;
                        SetStage(em, e, day, age, ref s);
                    }
                    if (s.m_Conditioning < 100u) s.m_Conditioning++;
                    SoulFeel.Pulse(em, e, SoulTune.SportPracticeMood);
                }
            }

            // D. games & streaks (F72): winner stays on until they don't.
            if ((s.m_Sport == SoulSportKind.Basketball || s.m_Sport == SoulSportKind.Soccer ||
                 s.m_Sport == SoulSportKind.Esports || s.m_Sport == SoulSportKind.Chess ||
                 s.m_Sport == SoulSportKind.MartialArts) &&
                Roll(day, salt + 13u, 1000u) < (uint)SoulTune.SportGamePerMille)
            {
                s.m_Games++;
                uint win = (uint)(SoulTune.SportWinBase + (int)s.m_Skill * 4 + (fitness ? 40 : 0));
                if (win > 850u) win = 850u;
                if (Roll(day, salt + 17u, 1000u) < win)
                {
                    s.m_Wins++;
                    s.m_Streak++;
                    if (s.m_Streak > s.m_Best) s.m_Best = s.m_Streak;
                    SoulFeel.Pulse(em, e, SoulTune.SportGameMood);
                }
                else
                {
                    s.m_Streak = 0u;
                }
                if (s.m_Sport == SoulSportKind.Basketball || s.m_Sport == SoulSportKind.Soccer)
                {
                    if ((s.m_Flags & SoulSportFlags.Crew) == 0 && s.m_Games >= (uint)SoulTune.SportCrewGames)
                    {
                        s.m_Flags |= SoulSportFlags.Crew;
                        SoulFeel.Note(em, e, day, SoulEventKind.CrewFormed, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.SportCrewMood);
                    }
                    if ((s.m_Flags & SoulSportFlags.Legend) == 0 && s.m_Best >= (uint)SoulTune.SportStreakLegend)
                    {
                        s.m_Flags |= SoulSportFlags.Legend;
                        SoulFeel.Note(em, e, day, SoulEventKind.PickupLegend, (short)s.m_Best, e);
                        SoulFeel.Pulse(em, e, SoulTune.SportLegendMood);
                    }
                }
                // a knock (F81): hard play takes its toll, rarely.
                uint hurt = (uint)(SoulTune.SportInjuryPerMille + 6 - (int)(s.m_Conditioning / 25u));
                if (Roll(day, salt + 19u, 1000u) < hurt)
                {
                    s.m_Flags |= SoulSportFlags.Sidelined;
                    s.m_Streak = 0u;
                    SoulFeel.Note(em, e, day, SoulEventKind.SportInjury, (short)s.m_Skill, e);
                    SoulFeel.Pulse(em, e, SoulTune.SportInjuryMood);
                }
            }

            // E. leagues & titles (F73/F79): seasons turn, and one day the cup.
            bool teamSport = s.m_Sport == SoulSportKind.Basketball || s.m_Sport == SoulSportKind.Soccer;
            if (age >= (uint)SoulTune.SportAdultDays && teamSport && s.m_Stage >= 1 &&
                day > (uint)SoulTune.SportSeasonDays)
            {
                uint seasonDay = day % (uint)SoulTune.SportSeasonDays;
                if (seasonDay == 310u && Roll(day, salt + 23u, 1000u) < (uint)SoulTune.SportTitleRunPerMille)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.TitleRun, (short)s.m_Skill, e);
                }
                if (seasonDay == 0u)
                {
                    if (s.m_Stage >= 2)
                    {
                        uint title = (uint)(SoulTune.SportTitlePerMille + (int)s.m_Skill / 4);
                        if (Roll(day, salt + 29u, 1000u) < title)
                        {
                            s.m_Flags |= SoulSportFlags.Champion;
                            SoulFeel.Note(em, e, day, SoulEventKind.TitleWon, (short)s.m_Skill, e);
                            SoulFeel.Note(em, e, day, SoulEventKind.ParadeDay, (short)s.m_Skill, e);
                            SoulFeel.Pulse(em, e, SoulTune.SportTitleMood);
                            SoulFeel.Pulse(em, e, SoulTune.SportParadeMood);
                        }
                    }
                    else if (Roll(day, salt + 31u, 1000u) < (uint)(SoulTune.SportLeagueTitlePerMille + (int)s.m_Skill / 5))
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.LeagueChamp, (short)s.m_Skill, e);
                        SoulFeel.Pulse(em, e, SoulTune.SportLeagueMood);
                    }
                }
            }

            // F. the long road (F74): the city marathon comes once a year.
            if (s.m_Sport == SoulSportKind.Running && s.m_Skill >= (uint)SoulTune.SportMarathonSkill &&
                day > 0u && day % 360u == 0u && Roll(day, salt + 37u, 1000u) < (uint)SoulTune.SportMarathonPerMille)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.Marathon, (short)s.m_Skill, e);
                SoulFeel.Pulse(em, e, SoulTune.SportMarathonMood);
                if (s.m_Conditioning < 100u) s.m_Conditioning = 100u;
            }

            // G. the dojo (F75): sparring days, and the odd hard night.
            if (s.m_Sport == SoulSportKind.MartialArts && s.m_Stage >= 1 &&
                Roll(day, salt + 43u, 1000u) < (uint)SoulTune.SportKnockoutPerMille)
            {
                s.m_Flags |= SoulSportFlags.Sidelined;
                s.m_Streak = 0u;
                SoulFeel.Note(em, e, day, SoulEventKind.Knockout, (short)s.m_Skill, e);
                SoulFeel.Pulse(em, e, SoulTune.SportKnockoutMood);
            }

            // H. the LAN night (F76): the ladder climbs to one loud evening.
            if (s.m_Sport == SoulSportKind.Esports && s.m_Skill >= (uint)SoulTune.SportLANSkill &&
                (day + salt) % 120u == 0u && Roll(day, salt + 47u, 1000u) < (uint)SoulTune.SportLANPerMille)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.LANChamp, (short)s.m_Skill, e);
                SoulFeel.Pulse(em, e, SoulTune.SportLANMood);
            }

            // I. derby day (F80): the whole city takes sides.
            if (age >= (uint)SoulTune.SportAdultDays && teamSport && (day + salt) % 300u == 0u &&
                Roll(day, salt + 53u, 1000u) < (uint)SoulTune.SportDerbyPerMille)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.DerbyDay, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.SportDerbyMood);
                if (Roll(day, salt + 59u, 1000u) < (uint)SoulTune.SportSplitPerMille)
                {
                    Entity kin = FindFriend(em, e, SoulTune.SportSplitMinAffinity);
                    if (kin != Entity.Null)
                    {
                        BumpBoth(em, e, kin, SoulTune.SportSplitBump);
                        SoulFeel.Note(em, e, day, SoulEventKind.SplitFamily, 1, kin);
                        SoulFeel.Note(em, kin, day, SoulEventKind.SplitFamily, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.SportSplitMood);
                    }
                }
            }

            // J. a rival (F77/F80): someone always stands across the board.
            if (s.m_Nemesis == Entity.Null && s.m_Games >= (uint)SoulTune.SportNemesisMinGames &&
                (teamSport || s.m_Sport == SoulSportKind.Chess || s.m_Sport == SoulSportKind.Esports) &&
                Roll(day, salt + 61u, 1000u) < (uint)SoulTune.SportNemesisPerMille)
            {
                Entity rival = FindFriend(em, e, SoulTune.SportNemesisMinAffinity);
                if (rival != Entity.Null)
                {
                    s.m_Nemesis = rival;
                    BumpBoth(em, e, rival, SoulTune.SportNemesisBump);
                    SoulFeel.Note(em, e, day, SoulEventKind.NemesisMet, 1, rival);
                    SoulFeel.Note(em, rival, day, SoulEventKind.NemesisMet, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.SportNemesisMood);
                }
            }

            // K. the kids (F78): scouts, and the day the cleats go in the closet.
            if (s.m_Sport != SoulSportKind.None && age < (uint)SoulTune.SportAdultDays)
            {
                if ((s.m_Flags & SoulSportFlags.Scouted) == 0 && s.m_Skill >= (uint)SoulTune.SportKidScoutSkill &&
                    (day + salt) % 30u == 0u && Roll(day, salt + 67u, 1000u) < (uint)SoulTune.SportKidScoutPerMille)
                {
                    s.m_Flags |= SoulSportFlags.Scouted;
                    SoulFeel.Note(em, e, day, SoulEventKind.KidScouted, (short)s.m_Skill, e);
                    SoulFeel.Pulse(em, e, SoulTune.SportKidStarMood);
                }
                if ((s.m_Flags & SoulSportFlags.Quit) == 0 && age >= (uint)SoulTune.SportKidGrownDays &&
                    s.m_Skill < (uint)SoulTune.SportKidQuitSkill)
                {
                    s.m_Flags |= SoulSportFlags.Quit;
                    s.m_Sport = SoulSportKind.None;
                    SoulFeel.Note(em, e, day, SoulEventKind.KidQuit, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.SportKidQuitMood);
                }
            }

            // L. the last season (F81): the body calls time; the stands remain.
            if (s.m_Sport != SoulSportKind.None && age >= (uint)SoulTune.SportRetireAgeDays &&
                (s.m_Flags & SoulSportFlags.Veteran) == 0)
            {
                s.m_Flags |= SoulSportFlags.Veteran;
                SoulFeel.Note(em, e, day, SoulEventKind.LastSeason, (short)s.m_Skill, e);
                SoulFeel.Pulse(em, e, SoulTune.SportLastSeasonMood);
            }
            else if (s.m_Sport != SoulSportKind.None && (s.m_Flags & SoulSportFlags.Veteran) != 0 &&
                (day + salt) % 300u == 0u)
            {
                s.m_Sport = SoulSportKind.None;
                SoulFeel.Note(em, e, day, SoulEventKind.SportRetired, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.SportRetireMood);
            }

            em.SetComponentData(e, s);
        }

        // ---- helpers --------------------------------------------------------

        private static void SetStage(EntityManager em, Entity e, uint day, uint age, ref SoulSport s)
        {
            byte want = 0;
            if (s.m_Skill >= (uint)SoulTune.SportStage4Skill) want = 4;
            else if (s.m_Skill >= (uint)SoulTune.SportStage3Skill) want = 3;
            else if (s.m_Skill >= (uint)SoulTune.SportStage2Skill) want = 2;
            else if (s.m_Skill >= (uint)SoulTune.SportStage1Skill) want = 1;
            if (want <= s.m_Stage) return;
            s.m_Stage = want;
            if (want == 1 && age >= (uint)SoulTune.SportAdultDays &&
                (s.m_Sport == SoulSportKind.Basketball || s.m_Sport == SoulSportKind.Soccer ||
                 s.m_Sport == SoulSportKind.Esports))
            {
                SoulFeel.Note(em, e, day, SoulEventKind.LeagueJoined, (short)s.m_Skill, e);
            }
            SoulFeel.Note(em, e, day, SoulEventKind.SportStageUp, want, e);
            SoulFeel.Pulse(em, e, SoulTune.SportStageMood);
        }

        private static byte PickSport(uint day, uint salt, bool fitness, bool hothead, bool loner)
        {
            if (loner)
            {
                uint solo = Roll(day, salt + 71u, 3u);
                if (solo == 0u) return SoulSportKind.Running;
                if (solo == 1u) return SoulSportKind.MartialArts;
                return SoulSportKind.Chess;
            }
            if (hothead && Roll(day, salt + 73u, 1000u) < 400u) return SoulSportKind.MartialArts;
            if (fitness && Roll(day, salt + 79u, 1000u) < 400u)
                return Roll(day, salt + 83u, 2u) == 0u ? SoulSportKind.Basketball : SoulSportKind.Soccer;
            return (byte)(Roll(day, salt + 89u, (uint)SoulSportKind.Count) + 1u);
        }

        private static Entity FindFriend(EntityManager em, Entity e, int minAffinity)
        {
            if (!em.HasBuffer<SoulEdge>(e)) return Entity.Null;
            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
            for (int i = 0; i < edges.Length; i++)
            {
                SoulEdge ed = edges[i];
                if (ed.m_Affinity < minAffinity) continue;
                Entity o = ed.m_Other;
                if (o == e) continue;
                if (!em.HasComponent<SoulMeta>(o)) continue;
                return o;
            }
            return Entity.Null;
        }

        private static void BumpBoth(EntityManager em, Entity e, Entity other, int delta)
        {
            BumpEdge(em, e, other, delta);
            BumpEdge(em, other, e, delta);
        }

        private static void BumpEdge(EntityManager em, Entity e, Entity other, int delta)
        {
            if (!em.HasBuffer<SoulEdge>(e)) return;
            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
            for (int i = 0; i < edges.Length; i++)
            {
                SoulEdge ed = edges[i];
                if (ed.m_Other != other) continue;
                int v = ed.m_Affinity + delta;
                if (v < -100) v = -100;
                if (v > 100) v = 100;
                ed.m_Affinity = (sbyte)v;
                edges[i] = ed;
                return;
            }
        }

        private static uint Roll(uint day, uint salt, uint span)
        {
            uint h = day * 2654435761u ^ salt * 2246822519u;
            h ^= h >> 15;
            h *= 3266489917u;
            h ^= h >> 13;
            return h % span;
        }
    }
}
