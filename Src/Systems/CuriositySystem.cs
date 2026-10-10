// Citizen Soul -- CuriositySystem (M5f): the learning life (F82-F90).
// Book clubs and library regulars; night classes that bond a cohort;
// second languages and kids translating for grandparents; museums and
// gallery openings; trivia teams and the board game that ended it all;
// stargazing; philosophy past midnight; apprenticeships to mastery day;
// and the small wonder of noticing the world.
// Reads quirks, personality and edges; writes only its own surfaces.
using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F82-F90: learning & curiosity -- clubs, classes, languages, museums,
    /// trivia, stars, late-night talk, apprenticeship, and noticing.
    /// </summary>
    public partial class CuriositySystem : SoulSystemBase
    {
        private const int CurioLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        /// <summary>128 lanes -> 1 pass/citizen/day.</summary>
        protected override int Lanes { get { return CurioLanes; } }

        private EntityQuery m_Curios;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Curios = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulCurio>());
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
                // Migration: every soul carries a curiosity book.
                if (m_All.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> all = m_All.ToEntityArray(Allocator.Temp);
                    int filled = 0;
                    for (int i = 0; i < all.Length && filled < MigrateBudget; i++)
                    {
                        Entity e = all[i];
                        if (em.HasComponent<SoulCurio>(e)) continue;
                        try
                        {
                            em.AddComponentData(e, SoulCurio.Fresh(day));
                            filled++;
                        }
                        catch (Exception ex)
                        {
                            SoulLog.Exception("curio.migrate." + e.Index, ex);
                        }
                    }
                    all.Dispose();
                }
            }
            catch (Exception ex)
            {
                SoulLog.Once("curio.migrate", "CuriositySystem: migration pass failed: " + ex.Message);
            }

            try
            {
                NativeArray<Entity> souls = m_Curios.ToEntityArray(Allocator.Temp);
                try
                {
                    int faults = 0;
                    for (int i = 0; i < souls.Length; i++)
                    {
                        Entity e = souls[i];
                        if (!Matches(e, lane, CurioLanes)) continue;
                        try
                        {
                            Advance(em, e, day);
                        }
                        catch (Exception ex)
                        {
                            faults++;
                            if (faults <= FaultLogCap) SoulLog.Exception("curio.fault." + e.Index, ex);
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
                SoulLog.Exception("curio.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulCurio>(e)) return;
            SoulCurio c = em.GetComponentData<SoulCurio>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            uint age = day >= meta.m_BirthDay ? day - meta.m_BirthDay : 0u;
            uint salt = (uint)e.Index * 2654435761u;

            byte openness = 50;
            byte conscientiousness = 100;
            bool hasPersonality = em.HasComponent<SoulPersonality>(e);
            SoulPersonality p = default(SoulPersonality);
            if (hasPersonality)
            {
                p = em.GetComponentData<SoulPersonality>(e);
                openness = p.m_Openness;
                conscientiousness = p.m_Conscientiousness;
            }

            if (age < (uint)SoulTune.CurioMinAgeDays)
            {
                em.SetComponentData(e, c);
                return;
            }

            // A. noticing (F90): small wonder, noted in the journal.
            if (Roll(day, salt + 3u, 1000u) < (uint)(SoulTune.CurioNoticePerMille + openness / 4))
            {
                SoulFeel.Note(em, e, day, SoulEventKind.Noticed, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.CurioNoticeMood);
            }

            // B. finding the hunger (F82-F89): something catches them.
            if (c.m_Track == SoulCurioTrack.None)
            {
                uint call = (uint)(SoulTune.CurioPickPerMille + openness / 5 + conscientiousness / 10);
                if (Roll(day, salt + 7u, 1000u) < call)
                {
                    byte track = PickTrack(day, salt, age, openness);
                    c.m_Track = track;
                    if (track == SoulCurioTrack.Library)
                    {
                        c.m_Flags |= SoulCurioFlags.Club;
                        SoulFeel.Note(em, e, day, SoulEventKind.BookClubJoined, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.CurioPickMood);
                    }
                    else if (track == SoulCurioTrack.NightClass)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.NightClass, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.CurioPickMood);
                    }
                    else if (track == SoulCurioTrack.Museum)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.MuseumVisit, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.CurioMuseumMood);
                    }
                    else if (track == SoulCurioTrack.Trivia)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.TriviaNight, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.CurioTriviaMood);
                    }
                    else if (track == SoulCurioTrack.Stargazer)
                    {
                        if (age < (uint)SoulTune.CurioTelescopeMaxDays)
                        {
                            SoulFeel.Note(em, e, day, SoulEventKind.TelescopeGift, 1, e);
                            SoulFeel.Pulse(em, e, SoulTune.CurioTelescopeMood);
                        }
                        else
                        {
                            SoulFeel.Note(em, e, day, SoulEventKind.Stargazing, 1, e);
                            SoulFeel.Pulse(em, e, SoulTune.CurioStarMood);
                        }
                    }
                    else if (track == SoulCurioTrack.Apprentice)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.ApprenticeJoined, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.CurioPickMood);
                    }
                    else
                    {
                        SoulFeel.Pulse(em, e, SoulTune.CurioPickMood);
                    }
                    // a familiar face for the tracks that need one
                    if (track == SoulCurioTrack.Library || track == SoulCurioTrack.NightClass ||
                        track == SoulCurioTrack.Trivia || track == SoulCurioTrack.Philosophy ||
                        track == SoulCurioTrack.Apprentice)
                    {
                        Entity mate = FindFriend(em, e, SoulTune.CurioPartnerMinAffinity);
                        if (mate != Entity.Null)
                        {
                            c.m_Partner = mate;
                            BumpBoth(em, e, mate, SoulTune.CurioPartnerBump);
                        }
                    }
                }
            }

            // C. evenings given to it (F82-F89): depth, sessions, the beats.
            if (c.m_Track != SoulCurioTrack.None && Roll(day, salt + 11u, 1000u) < (uint)(SoulTune.CurioSessionPerMille + openness / 5 + conscientiousness / 10))
            {
                if (c.m_Depth < 100u)
                {
                    c.m_Depth += (uint)SoulTune.CurioDepthGrow;
                    if (c.m_Depth > 100u) c.m_Depth = 100u;
                }
                c.m_Sessions++;
                if (c.m_Track == SoulCurioTrack.Library || c.m_Track == SoulCurioTrack.Museum) c.m_Books++;
                SoulFeel.Pulse(em, e, SoulTune.CurioSessionMood);

                if (c.m_Track == SoulCurioTrack.Library && (c.m_Flags & SoulCurioFlags.Schism) == 0 &&
                    c.m_Sessions >= (uint)SoulTune.CurioClubMinSessions && (day + salt) % 120u == 0u)
                {
                    c.m_Flags |= SoulCurioFlags.Schism;
                    SoulFeel.Note(em, e, day, SoulEventKind.BookClubSchism, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.CurioSchismMood);
                    if (c.m_Partner != Entity.Null) BumpBoth(em, e, c.m_Partner, SoulTune.CurioSchismBump);
                }
                if (c.m_Track == SoulCurioTrack.NightClass && (c.m_Flags & SoulCurioFlags.Cohort) == 0 &&
                    c.m_Sessions >= (uint)SoulTune.CurioCohortSessions)
                {
                    c.m_Flags |= SoulCurioFlags.Cohort;
                    if (c.m_Partner == Entity.Null) c.m_Partner = FindFriend(em, e, SoulTune.CurioPartnerMinAffinity);
                    SoulFeel.Note(em, e, day, SoulEventKind.CohortBond, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.CurioCohortMood);
                    if (c.m_Partner != Entity.Null) BumpBoth(em, e, c.m_Partner, SoulTune.CurioCohortBump);
                }
                if (c.m_Track == SoulCurioTrack.Language && (c.m_Flags & SoulCurioFlags.Bilingual) == 0 &&
                    c.m_Depth >= (uint)SoulTune.CurioFluencyDepth)
                {
                    c.m_Flags |= SoulCurioFlags.Bilingual;
                    SoulFeel.Note(em, e, day, SoulEventKind.LanguageFluency, (short)c.m_Depth, e);
                    SoulFeel.Pulse(em, e, SoulTune.CurioFluencyMood);
                }
                if (c.m_Track == SoulCurioTrack.Trivia)
                {
                    if ((c.m_Flags & SoulCurioFlags.Team) == 0 && c.m_Sessions >= (uint)SoulTune.CurioTriviaSessions)
                    {
                        c.m_Flags |= SoulCurioFlags.Team;
                        SoulFeel.Note(em, e, day, SoulEventKind.TriviaNight, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.CurioTriviaMood);
                    }
                    if ((c.m_Flags & SoulCurioFlags.Team) != 0 && (c.m_Flags & SoulCurioFlags.Feuded) == 0 &&
                        (day + salt) % 200u == 0u && Roll(day, salt + 13u, 1000u) < (uint)SoulTune.CurioFeudPerMille)
                    {
                        Entity mate = c.m_Partner != Entity.Null ? c.m_Partner : FindFriend(em, e, SoulTune.CurioPartnerMinAffinity);
                        if (mate != Entity.Null)
                        {
                            c.m_Flags |= SoulCurioFlags.Feuded;
                            BumpBoth(em, e, mate, SoulTune.CurioFeudBump);
                            SoulFeel.Note(em, e, day, SoulEventKind.BoardGameFeud, 1, mate);
                            SoulFeel.Note(em, mate, day, SoulEventKind.BoardGameFeud, 1, e);
                            SoulFeel.Pulse(em, e, SoulTune.CurioFeudMood);
                        }
                    }
                }
                if (c.m_Track == SoulCurioTrack.Philosophy)
                {
                    if ((day + salt) % 90u == 0u)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.LateNightTalk, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.CurioTalkMood);
                    }
                    if ((c.m_Flags & SoulCurioFlags.Argued) == 0 && c.m_Sessions >= (uint)SoulTune.CurioArgueSessions)
                    {
                        c.m_Flags |= SoulCurioFlags.Argued;
                        if (c.m_Partner == Entity.Null) c.m_Partner = FindFriend(em, e, SoulTune.CurioPartnerMinAffinity);
                        SoulFeel.Note(em, e, day, SoulEventKind.GreatArgument, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.CurioArgueMood);
                        if (c.m_Partner != Entity.Null)
                        {
                            BumpBoth(em, e, c.m_Partner, SoulTune.CurioArgueBump);
                            SoulFeel.Note(em, c.m_Partner, day, SoulEventKind.GreatArgument, 1, e);
                        }
                        if (hasPersonality && p.m_Openness < 100)
                        {
                            int op = p.m_Openness + 2;
                            if (op > 100) op = 100;
                            p.m_Openness = (byte)op;
                            em.SetComponentData(e, p);
                        }
                    }
                }
                if (c.m_Track == SoulCurioTrack.Apprentice && (c.m_Flags & SoulCurioFlags.Mastered) == 0 &&
                    c.m_Depth >= (uint)SoulTune.CurioMasteryDepth && c.m_Sessions >= (uint)SoulTune.CurioMasterySessions)
                {
                    c.m_Flags |= SoulCurioFlags.Mastered;
                    SoulFeel.Note(em, e, day, SoulEventKind.MasteryDay, (short)c.m_Depth, e);
                    SoulFeel.Pulse(em, e, SoulTune.CurioMasteryMood);
                    if (c.m_Partner != Entity.Null) BumpBoth(em, e, c.m_Partner, SoulTune.CurioMasteryBump);
                }
            }

            // E. museums & galleries (F85): the regulars, and the annual visit.
            if (c.m_Track == SoulCurioTrack.Museum)
            {
                if ((day + salt) % 150u == 0u && Roll(day, salt + 17u, 1000u) < (uint)SoulTune.CurioGalleryPerMille)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.GalleryOpening, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.CurioGalleryMood);
                    if (c.m_Partner != Entity.Null) BumpBoth(em, e, c.m_Partner, SoulTune.CurioGalleryBump);
                }
            }
            else if ((day + salt) % (uint)SoulTune.CurioAnnualMuseumGateDays == 0u &&
                Roll(day, salt + 19u, 1000u) < (uint)SoulTune.CurioAnnualMuseumPerMille)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.MuseumVisit, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.CurioMuseumMood);
                c.m_Books++;
            }

            // F. the watch (F87): meteor showers are city nights.
            if (c.m_Track == SoulCurioTrack.Stargazer && day > 0u && day % 300u == 0u)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.MeteorShower, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.CurioMeteorMood);
            }

            // G. translating for kin (F84): bilingual homes run through kids.
            if (age < (uint)SoulTune.CurioAdultDays && (day + salt) % (uint)SoulTune.CurioTranslateWindowDays == 0u &&
                Roll(day, salt + 23u, 1000u) < (uint)SoulTune.CurioTranslatePerMille)
            {
                Entity kin = FindBilingualKin(em, e);
                if (kin != Entity.Null)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.TranslatedForKin, 1, kin);
                    SoulFeel.Pulse(em, e, SoulTune.CurioTranslateMood);
                    BumpBoth(em, e, kin, SoulTune.CurioTranslateBump);
                }
            }

            em.SetComponentData(e, c);
        }

        // ---- helpers --------------------------------------------------------

        private static byte PickTrack(uint day, uint salt, uint age, byte openness)
        {
            if (age < (uint)SoulTune.CurioAdultDays)
            {
                if (age < (uint)SoulTune.CurioTeenDays)
                {
                    // kids: library, languages, museums, trivia, the stars
                    uint kid = Roll(day, salt + 71u, 5u);
                    if (kid == 0u) return SoulCurioTrack.Library;
                    if (kid == 1u) return SoulCurioTrack.Language;
                    if (kid == 2u) return SoulCurioTrack.Museum;
                    if (kid == 3u) return SoulCurioTrack.Trivia;
                    return SoulCurioTrack.Stargazer;
                }
                // teens: philosophy joins the list
                uint teen = Roll(day, salt + 73u, 6u);
                if (teen == 0u) return SoulCurioTrack.Library;
                if (teen == 1u) return SoulCurioTrack.Language;
                if (teen == 2u) return SoulCurioTrack.Museum;
                if (teen == 3u) return SoulCurioTrack.Trivia;
                if (teen == 4u) return SoulCurioTrack.Stargazer;
                return SoulCurioTrack.Philosophy;
            }
            // adults: open souls lean toward the talks, the stars, the galleries
            if (openness >= 65 && Roll(day, salt + 79u, 1000u) < 350u)
            {
                uint lean = Roll(day, salt + 83u, 3u);
                if (lean == 0u) return SoulCurioTrack.Philosophy;
                if (lean == 1u) return SoulCurioTrack.Stargazer;
                return SoulCurioTrack.Museum;
            }
            return (byte)(Roll(day, salt + 89u, (uint)SoulCurioTrack.Count) + 1u);
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

        private static Entity FindBilingualKin(EntityManager em, Entity e)
        {
            if (!em.HasBuffer<SoulEdge>(e)) return Entity.Null;
            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
            for (int i = 0; i < edges.Length; i++)
            {
                Entity o = edges[i].m_Other;
                if (o == e) continue;
                if (!em.HasComponent<SoulCurio>(o)) continue;
                if ((em.GetComponentData<SoulCurio>(o).m_Flags & SoulCurioFlags.Bilingual) == 0) continue;
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
