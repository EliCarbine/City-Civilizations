// Citizen Soul -- MediaSystem (M5l): screens, songs, and the crowd (F135-F141).
// The weekly show gets kept, the comfort one gets rewatched, and the
// hangover of a cliffhanger lingers. A decade claims a soul and songs
// own summers. Queues are fought for tickets, voices are lost in crowds,
// and someone from the crowd keeps showing up. The morning show learns
// their name; the headline finds their mood. A podcast gets pressed,
// grows, and maybe blows up. The big screen raises a cinephile, a critic,
// and a rainy night a deux. And at somebody's kitchen table, a campaign
// rolls dice until the finale. Extends F94's streaming habit (BingeNight,
// SpoilerCrime) rather than duplicating it.
using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F135-F141: one pass per citizen per day. Media is the background hum:
    /// weekly appointment TV, era-defining music, live crowds, and tabletop
    /// friendships counted in sessions.
    /// </summary>
    public partial class MediaSystem : SoulSystemBase
    {
        private const int MediaLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        /// <summary>128 lanes -> 1 pass/citizen/day.</summary>
        protected override int Lanes { get { return MediaLanes; } }

        private EntityQuery m_Media;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Media = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulMedia>());
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
                            if (em.HasComponent<SoulMedia>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulMedia.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("media.migrate." + e.Index, ex);
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
                SoulLog.Once("media.migrate", "MediaSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Media.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Media.ToEntityArray(Allocator.Temp);
                    int faults = 0;
                    try
                    {
                        for (int i = 0; i < souls.Length; i++)
                        {
                            Entity e = souls[i];
                            if (!em.Exists(e)) continue;
                            if (!Matches(e, lane, MediaLanes)) continue;
                            try
                            {
                                Advance(em, e, day);
                            }
                            catch (Exception ex)
                            {
                                faults++;
                                if (faults <= FaultLogCap) SoulLog.Exception("media.fault." + e.Index, ex);
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
                SoulLog.Exception("media.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulMedia>(e)) return;
            SoulMedia c = em.GetComponentData<SoulMedia>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            uint age = day >= meta.m_BirthDay ? day - meta.m_BirthDay : 0u;
            uint salt = (uint)e.Index * 2654435761u;

            byte openness = 50;
            byte neuroticism = 50;
            byte extraversion = 50;
            byte agreeableness = 50;
            if (em.HasComponent<SoulPersonality>(e))
            {
                SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
                openness = p.m_Openness;
                neuroticism = p.m_Neuroticism;
                extraversion = p.m_Extraversion;
                agreeableness = p.m_Agreeableness;
            }

            bool adult = age >= (uint)SoulTune.MediaMinAgeDays;

            // A. F135 -- the weekly show: appointment TV on top of F94's streaming.
            if (adult && (c.m_Flags & SoulMediaFlags.MustWatch) == 0 &&
                (day + salt) % 10u == 0u &&
                Roll(day, salt + 401u, 1000u) < (uint)SoulTune.MediaMustWatchPerMille)
            {
                c.m_Flags |= SoulMediaFlags.MustWatch;
                c.m_Show = (byte)(1u + Roll(day, salt + 402u, (uint)SoulTune.MediaShowSpan));
                SoulFeel.Note(em, e, day, SoulEventKind.MustWatchOn, (short)c.m_Show, e);
                SoulFeel.Pulse(em, e, SoulTune.MediaMustWatchMood);
            }
            if (adult && (c.m_Flags & SoulMediaFlags.MustWatch) != 0 &&
                (day + salt) % 14u == 0u &&
                Roll(day, salt + 403u, 1000u) < 400u)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.MustWatchOn, (short)c.m_Show, e);
                SoulFeel.Pulse(em, e, SoulTune.MediaMustWatchMood);
            }
            if (adult && (c.m_Flags & SoulMediaFlags.Rewatcher) == 0 &&
                (c.m_Flags & SoulMediaFlags.MustWatch) != 0 &&
                (day + salt) % 30u == 0u &&
                Roll(day, salt + 404u, 1000u) < (uint)SoulTune.MediaRewatchPerMille)
            {
                c.m_Flags |= SoulMediaFlags.Rewatcher;
                SoulFeel.Note(em, e, day, SoulEventKind.RewatchOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.MediaRewatchMood);
            }
            if (adult && (c.m_Flags & SoulMediaFlags.Cliffhanger) == 0 &&
                (c.m_Flags & SoulMediaFlags.MustWatch) != 0 &&
                (day + salt) % 21u == 0u &&
                Roll(day, salt + 405u, 1000u) < 300u)
            {
                c.m_Flags |= SoulMediaFlags.Cliffhanger;
                SoulFeel.Note(em, e, day, SoulEventKind.CliffhangerOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.MediaCliffMood);
            }
            if (adult && (c.m_Flags & SoulMediaFlags.Cliffhanger) != 0 &&
                (day + salt) % 45u == 0u &&
                Roll(day, salt + 406u, 1000u) < (uint)SoulTune.MediaJunkiePerMille)
            {
                Entity pal = FindFriend(em, e, 55);
                SoulFeel.Note(em, e, day, SoulEventKind.SpoilerCrime, 1, pal == Entity.Null ? e : pal);
                SoulFeel.Pulse(em, e, SoulTune.MediaCliffMood);
                if (pal != Entity.Null) BumpBoth(em, e, pal, -2);
            }

            // B. F136 -- music eras and the song of the summer.
            if (adult && (c.m_Flags & SoulMediaFlags.EraSet) == 0 &&
                (day + salt) % 30u == 0u &&
                Roll(day, salt + 411u, 1000u) < 500u)
            {
                c.m_Flags |= SoulMediaFlags.EraSet;
                c.m_Era = (byte)(1u + Roll(day, salt + 412u, (uint)SoulTune.MediaEraSpan));
                SoulFeel.Note(em, e, day, SoulEventKind.EraShiftOn, (short)c.m_Era, e);
                SoulFeel.Pulse(em, e, SoulTune.MediaEraMood);
            }
            if (adult && (c.m_Flags & SoulMediaFlags.EraSet) != 0 &&
                (day + salt) % 60u == 0u &&
                Roll(day, salt + 413u, 1000u) < (uint)SoulTune.MediaEraShiftPerMille)
            {
                Entity pal = FindFriend(em, e, SoulTune.MediaEraFriendSyncMin);
                if (pal != Entity.Null && em.HasComponent<SoulMedia>(pal))
                {
                    SoulMedia f = em.GetComponentData<SoulMedia>(pal);
                    if (f.m_Era != 0 && f.m_Era != c.m_Era)
                    {
                        c.m_Era = f.m_Era;
                        SoulFeel.Note(em, e, day, SoulEventKind.EraShiftOn, (short)c.m_Era, pal);
                        SoulFeel.Pulse(em, e, SoulTune.MediaEraMood);
                        BumpBoth(em, e, pal, 1);
                    }
                }
            }
            uint cycle = day / 90u;
            if (adult && (c.m_Flags & SoulMediaFlags.EraSet) != 0 &&
                c.m_LastSongSummer <= cycle &&
                Roll(cycle, salt + 414u, 1000u) < (uint)SoulTune.MediaSongSummerPerMille)
            {
                c.m_LastSongSummer = cycle + 1u;
                SoulFeel.Note(em, e, day, SoulEventKind.SongSummerOn, (short)(1u + Roll(day, salt + 415u, 20u)), e);
                SoulFeel.Pulse(em, e, SoulTune.MediaSongSummerMood);
            }

            // C. F137 -- live shows: the queue, the crowd, the shirt.
            if (adult &&
                (c.m_LastFestival == 0u || day >= c.m_LastFestival + (uint)SoulTune.MediaFestGapDays) &&
                (day + salt) % 30u == 0u &&
                Roll(day, salt + 421u, 1000u) < (uint)SoulTune.MediaTixPerMille)
            {
                c.m_LastFestival = day;
                SoulFeel.Note(em, e, day, SoulEventKind.TixChaosOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.MediaTixMood);
                if (Roll(day, salt + 422u, 1000u) < (uint)SoulTune.MediaTixSuccessPerMille)
                {
                    c.m_Flags |= SoulMediaFlags.ConcertGoer;
                    c.m_Flags |= SoulMediaFlags.FestGoer;
                    SoulFeel.Note(em, e, day, SoulEventKind.ConcertGoOn, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.MediaConcertMood);
                    if (c.m_Fandom == Entity.Null)
                    {
                        Entity f = FindFriend(em, e, 60);
                        if (f != Entity.Null)
                        {
                            c.m_Fandom = f;
                            BumpBoth(em, e, f, SoulTune.MediaFandomBump);
                        }
                    }
                }
                else
                {
                    SoulFeel.Pulse(em, e, SoulTune.MediaTixFailMood);
                }
            }
            if (adult && c.m_Fandom != Entity.Null && em.Exists(c.m_Fandom) &&
                (day + salt) % 45u == 0u &&
                Roll(day, salt + 423u, 1000u) < 300u)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.ConcertGoOn, 2, c.m_Fandom);
                BumpBoth(em, e, c.m_Fandom, 1);
                SoulFeel.Pulse(em, e, SoulTune.MediaConcertMood);
            }
            if (adult && (c.m_Flags & SoulMediaFlags.ConcertGoer) != 0 &&
                (day + salt) % 90u == 0u &&
                Roll(day, salt + 424u, 1000u) < (uint)SoulTune.MediaMerchPerMille)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.MerchOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.MediaMerchMood);
            }
            if (adult && (c.m_Flags & SoulMediaFlags.ConcertGoer) != 0 &&
                (day + salt) % 120u == 0u &&
                Roll(day, salt + 425u, 1000u) < (uint)SoulTune.MediaEncorePerMille)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.EncoreOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.MediaEncoreMood);
            }

            // D. F138 -- the morning show, the headline, and the radio in the aisles.
            if (adult && (c.m_Flags & SoulMediaFlags.MorningShow) == 0 &&
                (day + salt) % 60u == 0u &&
                Roll(day, salt + 431u, 1000u) < (uint)SoulTune.MediaMorningPerMille)
            {
                c.m_Flags |= SoulMediaFlags.MorningShow;
                SoulFeel.Note(em, e, day, SoulEventKind.MorningShowOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.MediaMorningMood);
            }
            if (adult && (c.m_Flags & SoulMediaFlags.MorningShow) != 0 &&
                (day + salt) % 30u == 0u &&
                Roll(day, salt + 432u, 1000u) < 300u)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.MorningShowOn, 2, e);
                SoulFeel.Pulse(em, e, SoulTune.MediaMorningMood);
            }
            if (adult && (day + salt) % 7u == 0u &&
                Roll(day, salt + 433u, 1000u) < ((c.m_Flags & SoulMediaFlags.NewsJunkie) != 0 ? 500u : 200u))
            {
                if (Roll(day, salt + 434u, 1000u) < 700u)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.HeadlineOn, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.MediaHeadlineGoodMood);
                }
                else
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.HeadlineOn, 0, e);
                    SoulFeel.Pulse(em, e, SoulTune.MediaHeadlineBadMood);
                }
                if ((c.m_Flags & SoulMediaFlags.NewsJunkie) == 0 &&
                    neuroticism >= (byte)SoulTune.MediaJunkieNeuroGateMin &&
                    Roll(day, salt + 435u, 1000u) < (uint)SoulTune.MediaJunkiePerMille)
                {
                    c.m_Flags |= SoulMediaFlags.NewsJunkie;
                }
            }
            if (adult && (day + salt) % 45u == 0u &&
                Roll(day, salt + 436u, 1000u) < (uint)SoulTune.MediaShopRadioPerMille)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.ShopRadioOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.MediaShopRadioMood);
            }
            if (adult && (c.m_Flags2 & SoulMediaFlags2.Announcer) == 0 &&
                (day + salt) % 180u == 0u &&
                Roll(day, salt + 437u, 1000u) < (uint)SoulTune.MediaAnnouncerPerMille)
            {
                c.m_Flags2 |= SoulMediaFlags2.Announcer;
                SoulFeel.Note(em, e, day, SoulEventKind.AnnouncerOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.MediaAnnouncerMood);
            }

            // E. F139 -- the podcast: pressed, growing, and maybe blown up.
            if (adult && (c.m_Flags2 & SoulMediaFlags2.Podcaster) == 0 &&
                (day + salt) % 90u == 0u &&
                extraversion >= (byte)SoulTune.MediaPodGoExtraGateMin &&
                Roll(day, salt + 441u, 1000u) < (uint)SoulTune.MediaPodGoPerMille)
            {
                c.m_Flags2 |= SoulMediaFlags2.Podcaster;
                c.m_PodStage = 1;
                SoulFeel.Note(em, e, day, SoulEventKind.PodcastGoOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.MediaPodGoMood);
            }
            if (adult && (c.m_Flags2 & SoulMediaFlags2.Podcaster) != 0 && c.m_PodStage == 1 &&
                (day + salt) % 60u == 0u &&
                Roll(day, salt + 442u, 1000u) < (uint)SoulTune.MediaPodGrowPerMille)
            {
                c.m_PodStage = 2;
                SoulFeel.Note(em, e, day, SoulEventKind.PodcastGrowOn, (short)(10u + Roll(day, salt + 443u, 490u)), e);
                SoulFeel.Pulse(em, e, SoulTune.MediaPodGrowMood);
            }
            if (adult && (c.m_Flags2 & SoulMediaFlags2.Podcaster) != 0 && c.m_PodStage == 2 &&
                (day + salt) % 90u == 0u &&
                Roll(day, salt + 444u, 1000u) < (uint)SoulTune.MediaPodBlowPerMille)
            {
                c.m_PodStage = 3;
                c.m_Flags2 |= SoulMediaFlags2.PodBlown;
                SoulFeel.Note(em, e, day, SoulEventKind.PodcastBlowOn, (short)(500u + Roll(day, salt + 445u, 500u)), e);
                SoulFeel.Pulse(em, e, SoulTune.MediaPodBlowMood);
            }
            if (adult && (c.m_Flags2 & SoulMediaFlags2.Podcaster) != 0 &&
                (day + salt) % 30u == 0u &&
                Roll(day, salt + 446u, 1000u) < (uint)SoulTune.MediaCommutePerMille)
            {
                c.m_Flags2 |= SoulMediaFlags2.Commuter;
                SoulFeel.Note(em, e, day, SoulEventKind.CommuteOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.MediaCommuteMood);
            }

            // F. F140 -- the big screen and the dark.
            if (adult && (c.m_Flags2 & SoulMediaFlags2.Cinephile) == 0 &&
                openness >= (byte)SoulTune.MediaCineOpenGateMin &&
                (day + salt) % 90u == 0u &&
                Roll(day, salt + 451u, 1000u) < (uint)SoulTune.MediaCinePerMille)
            {
                c.m_Flags2 |= SoulMediaFlags2.Cinephile;
                SoulFeel.Note(em, e, day, SoulEventKind.CinemaNightOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.MediaCineNightMood);
            }
            if (adult && (c.m_Flags2 & SoulMediaFlags2.Cinephile) != 0 &&
                (day + salt) % 21u == 0u &&
                Roll(day, salt + 452u, 1000u) < 300u)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.CinemaNightOn, 2, e);
                SoulFeel.Pulse(em, e, SoulTune.MediaCineNightMood);
            }
            if (adult && (c.m_Flags2 & SoulMediaFlags2.Cinephile) != 0 &&
                (c.m_Flags2 & SoulMediaFlags2.Critic) == 0 &&
                (day + salt) % 120u == 0u &&
                Roll(day, salt + 453u, 1000u) < (uint)SoulTune.MediaCriticPerMille)
            {
                c.m_Flags2 |= SoulMediaFlags2.Critic;
                SoulFeel.Note(em, e, day, SoulEventKind.CriticOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.MediaCriticMood);
            }
            if (adult && (day + salt) % 30u == 0u &&
                Roll(day, salt + 454u, 1000u) < (uint)SoulTune.MediaDateFilmPerMille)
            {
                Entity date = PartnerOrFriend(em, e);
                if (date != Entity.Null)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.DateFilmOn, 1, date);
                    BumpBoth(em, e, date, SoulTune.MediaDateFilmBump);
                    SoulFeel.Pulse(em, e, SoulTune.MediaDateFilmMood);
                }
            }
            if (adult && (day + salt) % 45u == 0u &&
                Roll(day, salt + 455u, 1000u) < (uint)SoulTune.MediaRainyPerMille)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.RainyFilmOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.MediaRainyMood);
            }
            if (adult && (day + salt) % 120u == 0u &&
                Roll(day, salt + 456u, 1000u) < (uint)SoulTune.MediaHomePremierePerMille)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.HomePremiereOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.MediaHomePremiereMood);
            }

            // G. F141 -- the kitchen table: a campaign counted in sessions.
            if (adult && c.m_CampaignMate == Entity.Null &&
                agreeableness >= (byte)SoulTune.MediaCampaignAgreeGateMin &&
                (day + salt) % 60u == 0u &&
                Roll(day, salt + 461u, 1000u) < (uint)SoulTune.MediaCampaignGoPerMille)
            {
                Entity f = FindFriend(em, e, 55);
                if (f != Entity.Null)
                {
                    c.m_CampaignMate = f;
                    SoulFeel.Note(em, e, day, SoulEventKind.CampaignGoOn, 1, f);
                    BumpBoth(em, e, f, 2);
                    SoulFeel.Pulse(em, e, SoulTune.MediaCampaignGoMood);
                }
            }
            if (adult && c.m_CampaignMate != Entity.Null && em.Exists(c.m_CampaignMate) &&
                day >= c.m_LastSessionDay + (uint)SoulTune.MediaSessionDays &&
                Roll(day, salt + 462u, 1000u) < (uint)SoulTune.MediaSessionPerMille)
            {
                c.m_LastSessionDay = day;
                c.m_Sessions++;
                SoulFeel.Note(em, e, day, SoulEventKind.SessionOn, (short)c.m_Sessions, c.m_CampaignMate);
                BumpBoth(em, e, c.m_CampaignMate, SoulTune.MediaSessionBump);
            }
            if (adult && c.m_CampaignMate != Entity.Null &&
                (c.m_Flags2 & SoulMediaFlags2.DM) == 0 &&
                c.m_Sessions >= (byte)SoulTune.MediaDMAfterSessions &&
                Roll(day, salt + 463u, 1000u) < (uint)SoulTune.MediaDMPerMille)
            {
                c.m_Flags2 |= SoulMediaFlags2.DM;
                SoulFeel.Note(em, e, day, SoulEventKind.DMRoleOn, 1, c.m_CampaignMate);
                SoulFeel.Pulse(em, e, SoulTune.MediaDMMood);
            }
            if (adult && c.m_CampaignMate != Entity.Null &&
                c.m_Sessions >= (byte)SoulTune.MediaFinaleSessions)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.FinaleOn, (short)c.m_Sessions, c.m_CampaignMate);
                BumpBoth(em, e, c.m_CampaignMate, SoulTune.MediaFinaleBump);
                SoulFeel.Pulse(em, e, SoulTune.MediaFinaleMood);
                c.m_Sessions = 0;
                c.m_LastSessionDay = day;
            }

            em.SetComponentData(e, c);
        }

        // ---- helpers --------------------------------------------------------
        private static Entity PartnerOrFriend(EntityManager em, Entity e)
        {
            if (em.HasComponent<SoulRomance>(e))
            {
                SoulRomance r = em.GetComponentData<SoulRomance>(e);
                if (r.m_Partner != Entity.Null && em.Exists(r.m_Partner)) return r.m_Partner;
            }
            return FindFriend(em, e, SoulTune.CurioPartnerMinAffinity);
        }

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
