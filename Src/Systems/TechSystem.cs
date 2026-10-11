// Citizen Soul -- TechSystem (M5g): phones, posts and the wired life (F91-F99).
// Screen habits and the fight they cause; a post that runs the town;
// group chats, inside jokes and the silence of a dead chat; binge nights,
// watch parties and spoiler crimes; gadget eras and holdouts; working from
// the cafe and missing the office; arcades, forums, and the digital memory
// a soul leaves behind.
// Reads personality and edges; writes only its own surfaces.
using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F91-F99: technology & modern life -- screens, posts, chats, streams,
    /// gadgets, remote work, arcades, forums, digital legacy.
    /// </summary>
    public partial class TechSystem : SoulSystemBase
    {
        private const int TechLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        /// <summary>128 lanes -> 1 pass/citizen/day.</summary>
        protected override int Lanes { get { return TechLanes; } }

        private EntityQuery m_Techs;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Techs = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulTech>());
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
                        if (em.HasComponent<SoulTech>(e)) continue;
                        try
                        {
                            em.AddComponentData(e, SoulTech.Fresh(day));
                            filled++;
                        }
                        catch (Exception ex)
                        {
                            SoulLog.Exception("tech.migrate." + e.Index, ex);
                        }
                    }
                    all.Dispose();
                }
            }
            catch (Exception ex)
            {
                SoulLog.Once("tech.migrate", "TechSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                NativeArray<Entity> souls = m_Techs.ToEntityArray(Allocator.Temp);
                try
                {
                    int faults = 0;
                    for (int i = 0; i < souls.Length; i++)
                    {
                        Entity e = souls[i];
                        if (!Matches(e, lane, TechLanes)) continue;
                        try
                        {
                            Advance(em, e, day);
                        }
                        catch (Exception ex)
                        {
                            faults++;
                            if (faults <= FaultLogCap) SoulLog.Exception("tech.fault." + e.Index, ex);
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
                SoulLog.Exception("tech.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulTech>(e)) return;
            SoulTech t = em.GetComponentData<SoulTech>(e);
            if (t.m_LastDay == day) return;
            t.m_LastDay = day;

            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            uint age = day >= meta.m_BirthDay ? day - meta.m_BirthDay : 0u;
            uint salt = (uint)e.Index * 2654435761u;

            byte openness = 50;
            byte neuroticism = 50;
            byte conscientiousness = 50;
            bool hasPersonality = em.HasComponent<SoulPersonality>(e);
            if (hasPersonality)
            {
                SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
                openness = p.m_Openness;
                neuroticism = p.m_Neuroticism;
                conscientiousness = p.m_Conscientiousness;
            }

            const uint teenDays = 4380u;   // ~12 years

            // A. the habit forms (F91): the anxious and the unbothered drift
            //    into the feed.
            if (age >= teenDays && (t.m_Flags & SoulTechFlags.ScreenHeavy) == 0 &&
                (neuroticism >= 65 || conscientiousness <= 35) &&
                Roll(day, salt + 3u, 1000u) < (uint)SoulTune.TechScreenFormPerMille)
            {
                t.m_Flags |= SoulTechFlags.ScreenHeavy;
                t.m_Screen = 60u;
            }

            // B. feed evenings & the fight about the phone (F91).
            if ((t.m_Flags & SoulTechFlags.ScreenHeavy) != 0)
            {
                if (t.m_Screen < 100u) t.m_Screen += 1u;
                if ((day + salt) % 60u == 0u && Roll(day, salt + 5u, 1000u) < 300u)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.Scrolling, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.TechScreenMood);
                }
                if ((day + salt) % 120u == 0u && Roll(day, salt + 7u, 1000u) < (uint)SoulTune.TechPhoneFightPerMille)
                {
                    Entity mate = FindFriend(em, e, SoulTune.CurioPartnerMinAffinity);
                    if (mate != Entity.Null)
                    {
                        BumpBoth(em, e, mate, SoulTune.TechPhoneFightBump);
                        SoulFeel.Note(em, e, day, SoulEventKind.PhoneFight, 1, mate);
                        SoulFeel.Note(em, mate, day, SoulEventKind.PhoneFight, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.TechScreenMood);
                    }
                }
            }

            // C. posting & the town (F92): little posts, and the one that
            //    runs the whole city.
            if (age >= teenDays && Roll(day, salt + 11u, 1000u) < (uint)SoulTune.TechPostPerMille)
            {
                t.m_Posts++;
                uint gain = (uint)SoulTune.TechPostFollowerGain;
                if (em.HasComponent<SoulArts>(e))
                {
                    gain += (uint)(em.GetComponentData<SoulArts>(e).m_Renown / 20);
                }
                t.m_Followers += gain;
                SoulFeel.Note(em, e, day, SoulEventKind.Posted, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.TechChatMood);
                if (t.m_Posts >= 10u && (t.m_Flags & SoulTechFlags.Viral) == 0 &&
                    Roll(day, salt + 13u, 1000u) < (uint)SoulTune.TechViralPerMille)
                {
                    t.m_Flags |= SoulTechFlags.Viral;
                    t.m_Followers += (uint)SoulTune.TechViralFollowers;
                    SoulFeel.Note(em, e, day, SoulEventKind.ViralPost, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.TechViralMood);
                }
            }

            // D. the group chat (F93): it comes alive, jokes leak, and one
            //    day nobody answers.
            if (age >= teenDays && (day + salt) % (uint)SoulTune.TechChatWindowDays == 0u)
            {
                if ((t.m_Flags & SoulTechFlags.Chat) == 0)
                {
                    if (Roll(day, salt + 17u, 1000u) < 400u)
                    {
                        Entity mate = FindFriend(em, e, SoulTune.CurioPartnerMinAffinity);
                        if (mate != Entity.Null)
                        {
                            t.m_Flags |= SoulTechFlags.Chat;
                            t.m_Mate = mate;
                            BumpBoth(em, e, mate, SoulTune.TechForumBump);
                            SoulFeel.Note(em, e, day, SoulEventKind.ChatFormed, 1, mate);
                            SoulFeel.Pulse(em, e, SoulTune.TechChatMood);
                        }
                    }
                }
                else
                {
                    uint pick = Roll(day, salt + 19u, 100u);
                    if (pick < 50u)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.ChatJoke, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.TechChatMood);
                    }
                    else if (pick < 65u)
                    {
                        Entity mate = t.m_Mate != Entity.Null && em.Exists(t.m_Mate) ? t.m_Mate : Entity.Null;
                        if (mate != Entity.Null)
                        {
                            BumpBoth(em, e, mate, SoulTune.TechChatQuietBump);
                        }
                        SoulFeel.Note(em, e, day, SoulEventKind.ChatQuiet, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.TechChatQuietMood);
                    }
                    else if (pick < 75u)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.LeftOnRead, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.TechReadMood);
                    }
                }
            }

            // E. streaming (F94): the habit, the watch party, the spoiler,
            //    and the show the whole city follows.
            if (age >= teenDays && (t.m_Flags & SoulTechFlags.Binger) == 0 &&
                Roll(day, salt + 23u, 1000u) < (uint)SoulTune.TechBingeFormPerMille)
            {
                t.m_Flags |= SoulTechFlags.Binger;
                SoulFeel.Note(em, e, day, SoulEventKind.BingeNight, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.TechBingeMood);
            }
            if ((t.m_Flags & SoulTechFlags.Binger) != 0 && (day + salt) % 30u == 0u)
            {
                uint pick = Roll(day, salt + 29u, 1000u);
                if (pick < 200u)
                {
                    Entity mate = FindFriend(em, e, SoulTune.CurioPartnerMinAffinity);
                    SoulFeel.Note(em, e, day, SoulEventKind.WatchParty, 1, mate);
                    SoulFeel.Pulse(em, e, SoulTune.TechWatchPartyMood);
                    if (mate != Entity.Null)
                    {
                        BumpBoth(em, e, mate, SoulTune.TechWatchPartyBump);
                        SoulFeel.Note(em, mate, day, SoulEventKind.WatchParty, 1, e);
                    }
                }
                else if (pick < 200u + (uint)SoulTune.TechSpoilerPerMille)
                {
                    Entity mate = FindFriend(em, e, SoulTune.CurioPartnerMinAffinity);
                    if (mate != Entity.Null)
                    {
                        BumpBoth(em, e, mate, SoulTune.TechSpoilerBump);
                        SoulFeel.Note(em, e, day, SoulEventKind.SpoilerCrime, 1, mate);
                        SoulFeel.Note(em, mate, day, SoulEventKind.SpoilerCrime, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.TechSpoilerMood);
                    }
                }
            }
            if (day > 0u && day % 180u == 0u && Roll(day, salt + 31u, 100u) < 40u)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.EveryoneWatching, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.TechEveryoneMood);
            }

            // F. gadget eras & holdouts (F95): first in line, stubbornly
            //    behind, or grumbling on the fence.
            if (age >= teenDays && (day + salt) % 300u == 0u)
            {
                if (openness >= 65)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.NewGadget, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.TechGadgetMood);
                }
                else if (openness <= 35)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.TechAbstain, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.TechAbstainMood);
                }
                else if (Roll(day, salt + 37u, 100u) < 50u)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.GadgetGrumble, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.TechGrumbleMood);
                }
            }
            if (day > 0u && day % 365u == 165u)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.TechEra, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.TechEraMood);
            }

            // G. working from the cafe (F96): and missing the old office.
            if (age >= 6570u && (t.m_Flags & SoulTechFlags.Remote) == 0 &&
                (day + salt) % 120u == 0u &&
                Roll(day, salt + 41u, 1000u) < (uint)SoulTune.TechRemoteFormPerMille)
            {
                t.m_Flags |= SoulTechFlags.Remote;
                SoulFeel.Note(em, e, day, SoulEventKind.RemoteWork, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.TechRemoteMood);
            }
            if ((t.m_Flags & SoulTechFlags.Remote) != 0 && (day + salt) % 200u == 0u &&
                Roll(day, salt + 43u, 100u) < 15u)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.LonelyRemote, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.TechLonelyMood);
            }

            // H. arcades (F97): ranked nights, the kid nobody beats, and
            //    the day the doors close for good.
            if (age >= (uint)SoulTune.TechArcadeMinAgeDays && age < 6570u &&
                (t.m_Flags & SoulTechFlags.Arcader) == 0 &&
                (day + salt) % 150u == 0u && Roll(day, salt + 47u, 100u) < 25u)
            {
                t.m_Flags |= SoulTechFlags.Arcader;
                SoulFeel.Note(em, e, day, SoulEventKind.ArcadeNight, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.TechArcadeMood);
            }
            if ((t.m_Flags & SoulTechFlags.Arcader) != 0)
            {
                if (age < 4380u && (day + salt) % 90u == 0u && Roll(day, salt + 53u, 100u) < 20u)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.ArcadeKid, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.TechArcadeKidMood);
                }
                if ((day + salt) % 365u == 0u && Roll(day, salt + 59u, 100u) < 30u)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.ArcadeNight, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.TechArcadeMood);
                }
                if (day > 0u && day % 500u == 0u && Roll(day, salt + 61u, 100u) < 25u)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.ArcadeClosed, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.TechArcadeCloseMood);
                }
            }

            // I. forums (F98): the niche corner of the net, and the unlikely
            //    friendship that walks out of it.
            if (age >= 4380u && (t.m_Flags & SoulTechFlags.Forum) == 0 &&
                (day + salt) % 210u == 0u && Roll(day, salt + 67u, 100u) < 20u)
            {
                t.m_Flags |= SoulTechFlags.Forum;
                Entity mate = FindFriend(em, e, SoulTune.CurioPartnerMinAffinity);
                if (mate != Entity.Null)
                {
                    t.m_Mate = mate;
                    BumpBoth(em, e, mate, SoulTune.TechForumBump);
                }
                SoulFeel.Note(em, e, day, SoulEventKind.ForumJoined, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.TechForumMood);
            }
            if ((t.m_Flags & SoulTechFlags.Forum) != 0 && t.m_Mate != Entity.Null && em.Exists(t.m_Mate) &&
                (day + salt) % 150u == 0u &&
                Roll(day, salt + 71u, 1000u) < (uint)SoulTune.TechForumVisitPerMille)
            {
                BumpBoth(em, e, t.m_Mate, SoulTune.TechForumVisitBump);
                SoulFeel.Note(em, e, day, SoulEventKind.ForumFriend, 1, t.m_Mate);
                SoulFeel.Pulse(em, e, SoulTune.TechForumMood);
            }

            // J. the digital memory (F99): the posts will outlive them.
            if (age >= (uint)SoulTune.TechLegacyAgeDays && (t.m_Flags & SoulTechFlags.Legacy) == 0)
            {
                t.m_Flags |= SoulTechFlags.Legacy;
                SoulFeel.Note(em, e, day, SoulEventKind.DigitalLegacy, (short)t.m_Posts, e);
                SoulFeel.Pulse(em, e, SoulTune.TechLegacyMood);
            }

            em.SetComponentData(e, t);
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
