// Citizen Soul -- QuirksSystem (M5j): the little oddities that make a
// person (F117-F124) -- superstitions, collections, one big fear, a
// catchphrase, keeping stats, building little systems, a comfort object,
// and a tradition that is theirs alone.
using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F117-F124: every soul earns a few quirks -- the lucky ritual it
    /// never skips, the collection it keeps, the fear it routes around,
    /// the phrase it coined, the numbers it logs, the charm in its
    /// pocket, the tradition it keeps every year.
    /// </summary>
    public partial class QuirksSystem : SoulSystemBase
    {
        private const int QuirkLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        /// <summary>128 lanes -> 1 pass/citizen/day.</summary>
        protected override int Lanes { get { return QuirkLanes; } }

        private EntityQuery m_Quirks;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Quirks = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulQuirk>());
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
                    int budget = 0;
                    try
                    {
                        for (int i = 0; i < all.Length; i++)
                        {
                            if (budget >= MigrateBudget) break;
                            Entity e = all[i];
                            if (!em.Exists(e)) continue;
                            if (em.HasComponent<SoulQuirk>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulQuirk.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("quirks.migrate." + e.Index, ex);
                            }
                        }
                    }
                    finally
                    {
                        all.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                SoulLog.Once("quirks.migrate", "QuirksSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Quirks.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Quirks.ToEntityArray(Allocator.Temp);
                    int faults = 0;
                    try
                    {
                        for (int i = 0; i < souls.Length; i++)
                        {
                            Entity e = souls[i];
                            if (!em.Exists(e)) continue;
                            if (!Matches(e, lane, QuirkLanes)) continue;
                            try
                            {
                                Advance(em, e, day);
                            }
                            catch (Exception ex)
                            {
                                faults++;
                                if (faults <= FaultLogCap) SoulLog.Exception("quirks.fault." + e.Index, ex);
                            }
                        }
                    }
                    finally
                    {
                        souls.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                SoulLog.Exception("quirks.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulQuirk>(e)) return;
            SoulQuirk c = em.GetComponentData<SoulQuirk>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            uint age = day >= meta.m_BirthDay ? day - meta.m_BirthDay : 0u;
            uint salt = (uint)e.Index * 2654435761u;
            uint doy = day % 360u;

            byte openness = 50;
            byte agreeableness = 50;
            byte order = 50;
            byte voice = 50;
            byte worry = 50;
            if (em.HasComponent<SoulPersonality>(e))
            {
                SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
                openness = p.m_Openness;
                agreeableness = p.m_Agreeableness;
                order = p.m_Conscientiousness;
                voice = p.m_Extraversion;
                worry = p.m_Neuroticism;
            }

            // A. superstitions (F117): a lucky ritual forms, pays off once,
            //    gets blamed when skipped, and spreads through friends.
            if (age >= (uint)SoulTune.QuirkMinAgeDays && (c.m_Flags & SoulQuirkFlags.Ritual) == 0 &&
                (openness >= SoulTune.QuirkOpenGateMin || Roll(day, salt + 3u, 100u) < 30u) &&
                (day + salt) % 60u == 0u && Roll(day, salt + 5u, 1000u) < (uint)SoulTune.QuirkRitualFormPerMille)
            {
                c.m_Flags |= SoulQuirkFlags.Ritual;
                SoulFeel.Note(em, e, day, SoulEventKind.RitualOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.QuirkRitualMood);
            }
            if ((c.m_Flags & SoulQuirkFlags.Ritual) != 0 && (day + salt) % 90u == 0u)
            {
                if (Roll(day, salt + 7u, 1000u) < (uint)SoulTune.QuirkBigDayPerMille)
                {
                    bool firstWin = (c.m_Flags2 & SoulQuirkFlags2.LuckyWin) == 0;
                    c.m_Flags2 |= SoulQuirkFlags2.LuckyWin;
                    SoulFeel.Pulse(em, e, SoulTune.QuirkLuckyMood);
                    if (firstWin)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.LuckyWin, 1, e);
                    }
                }
                else
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.RitualBlame, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.QuirkBlameMood);
                }
            }
            if ((c.m_Flags & SoulQuirkFlags.Ritual) != 0 && (c.m_Flags3 & SoulQuirkFlags3.RitualShared) == 0 &&
                (day + salt) % 45u == 0u && Roll(day, salt + 11u, 1000u) < (uint)SoulTune.QuirkSpreadPerMille)
            {
                Entity buddy = FindFriend(em, e, SoulTune.CurioPartnerMinAffinity);
                if (buddy != Entity.Null)
                {
                    c.m_Flags3 |= SoulQuirkFlags3.RitualShared;
                    BumpBoth(em, e, buddy, SoulTune.QuirkSpreadBump);
                    SoulFeel.Note(em, e, day, SoulEventKind.RitualShared, 1, buddy);
                    SoulFeel.Pulse(em, e, SoulTune.QuirkSpreadMood);
                }
            }

            // B. collections (F118): a shelf of small finds, the miss, the
            //    day the last piece lands.
            if (age >= (uint)SoulTune.QuirkMinAgeDays && (c.m_Flags & SoulQuirkFlags.Collector) == 0 &&
                (openness >= SoulTune.QuirkOpenGateMin || Roll(day, salt + 13u, 100u) < 25u) &&
                (day + salt) % 90u == 0u && Roll(day, salt + 17u, 1000u) < (uint)SoulTune.QuirkCollectFormPerMille)
            {
                c.m_Flags |= SoulQuirkFlags.Collector;
                c.m_Pieces = 1u;
                SoulFeel.Note(em, e, day, SoulEventKind.CollectOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.QuirkCollectMood);
            }
            if ((c.m_Flags & SoulQuirkFlags.Collector) != 0 && (day + salt) % 45u == 0u)
            {
                if (c.m_Pieces < 60000u) c.m_Pieces++;
                SoulFeel.Note(em, e, day, SoulEventKind.CollectFind, (short)c.m_Pieces, e);
                SoulFeel.Pulse(em, e, SoulTune.QuirkFindMood);
                if (Roll(day, salt + 19u, 1000u) < (uint)SoulTune.QuirkMissPerMille)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.CollectMiss, (short)c.m_Pieces, e);
                    SoulFeel.Pulse(em, e, SoulTune.QuirkMissMood);
                }
                if ((c.m_Flags2 & SoulQuirkFlags2.SetDone) == 0 && c.m_Pieces >= (uint)SoulTune.QuirkSetPieces)
                {
                    c.m_Flags2 |= SoulQuirkFlags2.SetDone;
                    SoulFeel.Note(em, e, day, SoulEventKind.SetDone, (short)c.m_Pieces, e);
                    SoulFeel.Pulse(em, e, SoulTune.QuirkSetMood);
                }
            }

            // C. fears (F119): one big fear gets a name; they route around
            //    it, freeze once, then a friend helps them face it.
            if (age >= (uint)SoulTune.QuirkMinAgeDays && (c.m_Flags & SoulQuirkFlags.Fearful) == 0 &&
                (worry >= SoulTune.QuirkAnxGateMin || Roll(day, salt + 23u, 100u) < 25u) &&
                (day + salt) % 90u == 0u && Roll(day, salt + 29u, 1000u) < (uint)SoulTune.QuirkFearFormPerMille)
            {
                c.m_Flags |= SoulQuirkFlags.Fearful;
                c.m_Fear = (byte)(1u + Roll(day, salt + 31u, 8u));
                SoulFeel.Note(em, e, day, SoulEventKind.FearOn, (short)c.m_Fear, e);
                SoulFeel.Pulse(em, e, SoulTune.QuirkFearMood);
            }
            if ((c.m_Flags & SoulQuirkFlags.Fearful) != 0 && (day + salt) % 21u == 0u)
            {
                if (Roll(day, salt + 37u, 1000u) < (uint)SoulTune.QuirkFreezePerMille)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.FearFreeze, (short)c.m_Fear, e);
                    SoulFeel.Pulse(em, e, SoulTune.QuirkFreezeMood);
                }
                else if (Roll(day, salt + 41u, 1000u) < (uint)SoulTune.QuirkRoutePerMille)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.FearRoute, (short)c.m_Fear, e);
                    SoulFeel.Pulse(em, e, SoulTune.QuirkRouteMood);
                }
            }
            if ((c.m_Flags & SoulQuirkFlags.Fearful) != 0 && (c.m_Flags3 & SoulQuirkFlags3.Exposure) == 0 &&
                (day + salt) % 120u == 0u)
            {
                Entity pal = FindFriend(em, e, SoulTune.CurioPartnerMinAffinity);
                if (pal != Entity.Null)
                {
                    c.m_Flags3 |= SoulQuirkFlags3.Exposure;
                    BumpBoth(em, e, pal, SoulTune.QuirkExposureBump);
                    SoulFeel.Note(em, e, day, SoulEventKind.ExposureArc, (short)c.m_Fear, pal);
                    SoulFeel.Pulse(em, e, SoulTune.QuirkExposureMood);
                }
            }
            if ((c.m_Flags & SoulQuirkFlags.Fearful) != 0 && (c.m_Flags3 & SoulQuirkFlags3.Exposure) != 0 &&
                (c.m_Flags2 & SoulQuirkFlags2.FearShrunk) == 0 && (day + salt) % 60u == 0u &&
                Roll(day, salt + 43u, 1000u) < (uint)SoulTune.QuirkShrinkPerMille)
            {
                c.m_Flags2 |= SoulQuirkFlags2.FearShrunk;
                SoulFeel.Note(em, e, day, SoulEventKind.FearShrunk, (short)c.m_Fear, e);
                SoulFeel.Pulse(em, e, SoulTune.QuirkShrinkMood);
            }

            // D. speech (F120): a catchphrase gets coined, a friend gets a
            //    nickname, the phrase comes back around, goes city-wide.
            if (age >= (uint)SoulTune.QuirkMinAgeDays && (c.m_Flags & SoulQuirkFlags.Phrase) == 0 &&
                (voice >= SoulTune.QuirkVoiceGateMin || Roll(day, salt + 47u, 100u) < 30u) &&
                (day + salt) % 60u == 0u && Roll(day, salt + 53u, 1000u) < (uint)SoulTune.QuirkPhraseFormPerMille)
            {
                c.m_Flags |= SoulQuirkFlags.Phrase;
                c.m_Word = (byte)(1u + Roll(day, salt + 59u, 16u));
                SoulFeel.Note(em, e, day, SoulEventKind.PhraseCoin, (short)c.m_Word, e);
                SoulFeel.Pulse(em, e, SoulTune.QuirkPhraseMood);
            }
            if ((c.m_Flags & SoulQuirkFlags.Phrase) != 0 && (c.m_Flags3 & SoulQuirkFlags3.Nicknamed) == 0)
            {
                Entity pal = FindFriend(em, e, SoulTune.CurioPartnerMinAffinity);
                if (pal != Entity.Null)
                {
                    c.m_Companion = pal;
                    c.m_Flags3 |= SoulQuirkFlags3.Nicknamed;
                    BumpBoth(em, e, pal, SoulTune.QuirkNickBump);
                    SoulFeel.Note(em, e, day, SoulEventKind.Nickname, (short)c.m_Word, pal);
                    SoulFeel.Pulse(em, e, SoulTune.QuirkNickMood);
                }
            }
            if ((c.m_Flags & SoulQuirkFlags.Phrase) != 0 && (day + salt) % 90u == 0u)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.PhraseSpread, (short)c.m_Word, e);
                SoulFeel.Pulse(em, e, SoulTune.QuirkPhraseSpreadMood);
                if ((c.m_Flags2 & SoulQuirkFlags2.SlangCity) == 0 && (day + salt) % 360u == 0u &&
                    Roll(day, salt + 61u, 1000u) < (uint)SoulTune.QuirkSlangPerMille)
                {
                    c.m_Flags2 |= SoulQuirkFlags2.SlangCity;
                    SoulFeel.Note(em, e, day, SoulEventKind.SlangCity, (short)c.m_Word, e);
                    SoulFeel.Pulse(em, e, SoulTune.QuirkSlangMood);
                }
            }

            // E. stat-keeping (F121): keeps notebooks of numbers; the day
            //    the archive asks for them.
            if ((c.m_Flags & SoulQuirkFlags.StatKeeper) == 0)
            {
                if (age >= (uint)SoulTune.QuirkMinAgeDays && order >= SoulTune.QuirkOrderGateMin &&
                    Roll(day, salt + 67u, 1000u) < (uint)SoulTune.QuirkStatFormPerMille)
                {
                    c.m_Flags |= SoulQuirkFlags.StatKeeper;
                    SoulFeel.Note(em, e, day, SoulEventKind.StatOn, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.QuirkStatMood);
                }
            }
            else
            {
                if ((day + salt) % 30u == 0u && Roll(day, salt + 71u, 1000u) < (uint)SoulTune.QuirkQuotePerMille)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.StatQuote, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.QuirkQuoteMood);
                }
                if ((c.m_Flags2 & SoulQuirkFlags2.Archived) == 0 && doy == (uint)SoulTune.QuirkArchiveDay)
                {
                    c.m_Flags2 |= SoulQuirkFlags2.Archived;
                    SoulFeel.Note(em, e, day, SoulEventKind.StatArchive, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.QuirkArchiveMood);
                }
            }

            // F. lists (F122): little systems to keep the days tidy.
            if ((c.m_Flags & SoulQuirkFlags.Lister) == 0)
            {
                if (age >= (uint)SoulTune.QuirkMinAgeDays &&
                    Roll(day, salt + 73u, 1000u) < (uint)SoulTune.QuirkListFormPerMille)
                {
                    c.m_Flags |= SoulQuirkFlags.Lister;
                    SoulFeel.Note(em, e, day, SoulEventKind.ListOn, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.QuirkListMood);
                }
            }
            else
            {
                if ((day + salt) % 7u == 0u)
                {
                    if (Roll(day, salt + 79u, 1000u) < (uint)SoulTune.QuirkWinPerMille)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.ListWin, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.QuirkWinMood);
                    }
                    else if (Roll(day, salt + 83u, 1000u) < (uint)SoulTune.QuirkChaosPerMille)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.ListChaos, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.QuirkChaosMood);
                    }
                }
            }

            // G. charms (F123): a comfort object in the pocket.
            if ((c.m_Flags & SoulQuirkFlags.CharmKeeper) == 0)
            {
                if (age >= (uint)SoulTune.QuirkMinAgeDays && agreeableness >= SoulTune.QuirkWarmGateMin &&
                    Roll(day, salt + 89u, 1000u) < (uint)SoulTune.QuirkCharmFormPerMille)
                {
                    c.m_Flags |= SoulQuirkFlags.CharmKeeper;
                    c.m_Charm = (byte)(1 + Roll(day, salt + 97u, 8u));
                    SoulFeel.Note(em, e, day, SoulEventKind.CharmOn, (short)c.m_Charm, e);
                    SoulFeel.Pulse(em, e, SoulTune.QuirkCharmMood);
                }
            }
            else
            {
                if ((c.m_Flags2 & SoulQuirkFlags2.CharmLost) == 0)
                {
                    if ((day + salt) % 20u == 0u && Roll(day, salt + 101u, 1000u) < (uint)SoulTune.QuirkCalmPerMille)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.CharmCalm, (short)c.m_Charm, e);
                        SoulFeel.Pulse(em, e, SoulTune.QuirkCalmMood);
                    }
                    if ((day + salt) % 150u == 0u && Roll(day, salt + 103u, 1000u) < (uint)SoulTune.QuirkLosePerMille)
                    {
                        c.m_Flags2 |= SoulQuirkFlags2.CharmLost;
                        SoulFeel.Note(em, e, day, SoulEventKind.CharmLost, (short)c.m_Charm, e);
                        SoulFeel.Pulse(em, e, SoulTune.QuirkLoseMood);
                    }
                }
                else if ((day + salt) % 30u == 0u && Roll(day, salt + 107u, 1000u) < (uint)SoulTune.QuirkBackPerMille)
                {
                    c.m_Flags2 = (byte)(c.m_Flags2 & ~SoulQuirkFlags2.CharmLost);
                    SoulFeel.Note(em, e, day, SoulEventKind.CharmBack, (short)c.m_Charm, e);
                    SoulFeel.Pulse(em, e, SoulTune.QuirkBackMood);
                }
            }

            // H. traditions (F124): one private tradition, kept yearly.
            if ((c.m_Flags & SoulQuirkFlags.Tradition) == 0)
            {
                if (age >= (uint)SoulTune.QuirkMinAgeDays &&
                    Roll(day, salt + 109u, 1000u) < (uint)SoulTune.QuirkTradFormPerMille)
                {
                    c.m_Flags |= SoulQuirkFlags.Tradition;
                    SoulFeel.Note(em, e, day, SoulEventKind.TradOn, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.QuirkTradMood);
                }
            }
            else
            {
                if (doy == (uint)SoulTune.QuirkTradDay)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.TradKeep, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.QuirkTradKeepMood);
                    if ((c.m_Flags3 & SoulQuirkFlags3.TradShared) == 0)
                    {
                        Entity pal = FindFriend(em, e, SoulTune.CurioPartnerMinAffinity);
                        if (pal != Entity.Null)
                        {
                            c.m_Flags3 |= SoulQuirkFlags3.TradShared;
                            BumpBoth(em, e, pal, SoulTune.QuirkShareBump);
                            SoulFeel.Note(em, e, day, SoulEventKind.TradShared, 1, pal);
                            SoulFeel.Pulse(em, e, SoulTune.QuirkShareMood);
                        }
                    }
                }
                if ((c.m_Flags2 & SoulQuirkFlags2.TradBroken) == 0 && doy == (uint)SoulTune.QuirkBreakDay &&
                    Roll(day, salt + 113u, 1000u) < (uint)SoulTune.QuirkBreakPerMille)
                {
                    c.m_Flags2 |= SoulQuirkFlags2.TradBroken;
                    SoulFeel.Note(em, e, day, SoulEventKind.TradBreak, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.QuirkBreakMood);
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
