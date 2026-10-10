// Citizen Soul -- NatureSystem (M5i): the green and the wild (F109-F116).
// Gardens and prized roses, bird life lists, trails and campfires, pier
// dawns, mushroom season, street cats and rescue glow-ups, weather that
// moves whole moods, and the fight for one old tree.
using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F109-F116: gives every soul a relationship with the green and the
    /// wild -- something grown, something watched, somewhere walked, the
    /// water, the woods, the strays, the sky, and one old tree.
    /// </summary>
    public partial class NatureSystem : SoulSystemBase
    {
        private const int NatureLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        /// <summary>128 lanes -> 1 pass/citizen/day.</summary>
        protected override int Lanes { get { return NatureLanes; } }

        private EntityQuery m_Nature;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Nature = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulNature>());
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
                            if (em.HasComponent<SoulNature>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulNature.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("nature.migrate." + e.Index, ex);
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
                SoulLog.Once("nature.migrate", "NatureSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Nature.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Nature.ToEntityArray(Allocator.Temp);
                    int faults = 0;
                    try
                    {
                        for (int i = 0; i < souls.Length; i++)
                        {
                            Entity e = souls[i];
                            if (!em.Exists(e)) continue;
                            if (!Matches(e, lane, NatureLanes)) continue;
                            try
                            {
                                Advance(em, e, day);
                            }
                            catch (Exception ex)
                            {
                                faults++;
                                if (faults <= FaultLogCap) SoulLog.Exception("nature.fault." + e.Index, ex);
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
                SoulLog.Exception("nature.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulNature>(e)) return;
            SoulNature c = em.GetComponentData<SoulNature>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            uint age = day >= meta.m_BirthDay ? day - meta.m_BirthDay : 0u;
            uint salt = (uint)e.Index * 2654435761u;
            uint doy = day % 360u;

            byte openness = 50;
            byte agreeableness = 50;
            bool hasPersonality = em.HasComponent<SoulPersonality>(e);
            if (hasPersonality)
            {
                SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
                openness = p.m_Openness;
                agreeableness = p.m_Agreeableness;
            }

            // A. gardens (F109): the open ones plant; thumbs are earned or
            //    lost; a prized rose; a cutting swapped with a friend.
            if (age >= (uint)SoulTune.NatureMinAgeDays && (c.m_Flags & SoulNatureFlags.Grower) == 0 &&
                (openness >= SoulTune.NatureOpenGateMin || Roll(day, salt + 3u, 100u) < 30u) &&
                (day + salt) % 60u == 0u && Roll(day, salt + 5u, 1000u) < (uint)SoulTune.NaturePlantFormPerMille)
            {
                c.m_Flags |= SoulNatureFlags.Grower;
                SoulFeel.Note(em, e, day, SoulEventKind.Planted, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.NaturePlantMood);
            }
            if ((c.m_Flags & SoulNatureFlags.Grower) != 0 && (day + salt) % 30u == 0u)
            {
                if (Roll(day, salt + 7u, 1000u) < (uint)SoulTune.NatureThumbPerMille)
                {
                    if (c.m_Luck < 200) c.m_Luck++;
                    SoulFeel.Note(em, e, day, SoulEventKind.GreenThumb, (short)c.m_Luck, e);
                    SoulFeel.Pulse(em, e, SoulTune.NatureThumbMood);
                }
                else
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.PlantMurder, (short)c.m_Luck, e);
                    SoulFeel.Pulse(em, e, SoulTune.NatureMurderMood);
                }
                if ((c.m_Flags2 & SoulNatureFlags2.RosePrized) == 0 &&
                    Roll(day, salt + 9u, 1000u) < (uint)SoulTune.NatureRosePerMille)
                {
                    c.m_Flags2 |= SoulNatureFlags2.RosePrized;
                    SoulFeel.Note(em, e, day, SoulEventKind.RosePrize, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.NatureRoseMood);
                }
                if (c.m_Buddy == Entity.Null)
                {
                    Entity bud = FindFriend(em, e, SoulTune.CurioPartnerMinAffinity);
                    if (bud != Entity.Null)
                    {
                        c.m_Buddy = bud;
                        BumpBoth(em, e, bud, SoulTune.NatureCuttingBump);
                        SoulFeel.Note(em, e, day, SoulEventKind.CuttingSwap, 1, bud);
                        SoulFeel.Pulse(em, e, SoulTune.NatureCuttingMood);
                    }
                }
            }

            // B. birding (F110): a life list grows by one, and the day the
            //    eagles came back.
            if (age >= (uint)SoulTune.NatureMinAgeDays && (c.m_Flags & SoulNatureFlags.Birder) == 0 &&
                (openness >= SoulTune.NatureOpenGateMin || Roll(day, salt + 11u, 100u) < 30u) &&
                (day + salt) % 90u == 0u && Roll(day, salt + 13u, 1000u) < (uint)SoulTune.NatureBirdFormPerMille)
            {
                c.m_Flags |= SoulNatureFlags.Birder;
                c.m_List = 1u;
                SoulFeel.Note(em, e, day, SoulEventKind.Birding, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.NatureBirdSpotMood);
            }
            if ((c.m_Flags & SoulNatureFlags.Birder) != 0 && (day + salt) % (uint)SoulTune.NatureBirdRecurDays == 0u)
            {
                if (c.m_List < 60000u) c.m_List++;
                SoulFeel.Note(em, e, day, SoulEventKind.BirdSpot, (short)c.m_List, e);
                SoulFeel.Pulse(em, e, SoulTune.NatureBirdSpotMood);
                if ((c.m_Flags2 & SoulNatureFlags2.EagleSeen) == 0 &&
                    Roll(day, salt + 17u, 1000u) < (uint)SoulTune.NatureEaglePerMille)
                {
                    c.m_Flags2 |= SoulNatureFlags2.EagleSeen;
                    SoulFeel.Note(em, e, day, SoulEventKind.EagleNest, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.NatureEagleMood);
                }
            }

            // C. trails (F111): weekend miles, a friend who keeps pace, one
            //    camp under it all, one story that outlives the hike.
            if (age >= (uint)SoulTune.NatureMinAgeDays && (c.m_Flags & SoulNatureFlags.Hiker) == 0 &&
                (openness >= SoulTune.NatureOpenGateMin || Roll(day, salt + 19u, 100u) < 30u) &&
                (day + salt) % 90u == 0u && Roll(day, salt + 23u, 1000u) < (uint)SoulTune.NatureHikeFormPerMille)
            {
                c.m_Flags |= SoulNatureFlags.Hiker;
                SoulFeel.Note(em, e, day, SoulEventKind.FirstHike, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.NatureHikeMood);
            }
            if ((c.m_Flags & SoulNatureFlags.Hiker) != 0 && (day + salt) % (uint)SoulTune.NatureHikeRecurDays == 0u)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.FirstHike, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.NatureHikeMood);
                if (c.m_Buddy == Entity.Null)
                {
                    Entity pal = FindFriend(em, e, SoulTune.CurioPartnerMinAffinity);
                    if (pal != Entity.Null)
                    {
                        c.m_Buddy = pal;
                        BumpBoth(em, e, pal, SoulTune.NatureTrailBump);
                        SoulFeel.Note(em, e, day, SoulEventKind.TrailFriend, 1, pal);
                        SoulFeel.Pulse(em, e, SoulTune.NatureTrailMood);
                    }
                }
            }
            if ((c.m_Flags & SoulNatureFlags.Hiker) != 0 && (day + salt) % (uint)(SoulTune.NatureHikeRecurDays * 2) == 0u)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.CampTrip, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.NatureCampMood);
                if ((c.m_Flags2 & SoulNatureFlags2.TripStory) == 0 &&
                    Roll(day, salt + 29u, 1000u) < (uint)SoulTune.NatureTripStoryPerMille)
                {
                    c.m_Flags2 |= SoulNatureFlags2.TripStory;
                    SoulFeel.Note(em, e, day, SoulEventKind.TripStory, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.NatureTripStoryMood);
                }
            }

            // D. the pier (F112): dawn casts, the one big one, and the
            //    retelling that grows it.
            if (age >= (uint)SoulTune.NatureMinAgeDays && (c.m_Flags & SoulNatureFlags.Angler) == 0 &&
                (openness >= SoulTune.NatureOpenGateMin || Roll(day, salt + 31u, 100u) < 25u) &&
                (day + salt) % 90u == 0u && Roll(day, salt + 37u, 1000u) < (uint)SoulTune.NatureFishFormPerMille)
            {
                c.m_Flags |= SoulNatureFlags.Angler;
                SoulFeel.Note(em, e, day, SoulEventKind.FishingOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.NatureCatchMood);
            }
            if ((c.m_Flags & SoulNatureFlags.Angler) != 0 && (day + salt) % (uint)SoulTune.NatureFishRecurDays == 0u)
            {
                if (Roll(day, salt + 41u, 1000u) < (uint)SoulTune.NatureCatchPerMille)
                {
                    if (c.m_Catch < 200) c.m_Catch++;
                    SoulFeel.Note(em, e, day, SoulEventKind.BigCatch, (short)c.m_Catch, e);
                    SoulFeel.Pulse(em, e, SoulTune.NatureCatchMood);
                    if (Roll(day, salt + 43u, 1000u) < (uint)SoulTune.NatureFishStoryPerMille)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.FishStory, (short)c.m_Catch, e);
                        SoulFeel.Pulse(em, e, SoulTune.NatureFishStoryMood);
                    }
                }
            }

            // E. mushroom season (F113): the patient learn the spots; some
            //    learn one spot too late.
            if (age >= (uint)SoulTune.NatureMinAgeDays && (c.m_Flags & SoulNatureFlags.Forager) == 0 &&
                (openness >= SoulTune.NatureOpenGateMin || Roll(day, salt + 47u, 100u) < 25u) &&
                (day + salt) % 120u == 0u && Roll(day, salt + 53u, 1000u) < (uint)SoulTune.NatureForageFormPerMille)
            {
                c.m_Flags |= SoulNatureFlags.Forager;
                SoulFeel.Note(em, e, day, SoulEventKind.ForageOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.NatureMushMood);
            }
            if ((c.m_Flags & SoulNatureFlags.Forager) != 0 &&
                doy >= (uint)SoulTune.NatureForageSeasonDay && doy < (uint)SoulTune.NatureForageSeasonDay + 30u &&
                (day + salt) % 2u == 0u)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.Mushroom, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.NatureMushMood);
                if ((c.m_Flags2 & SoulNatureFlags2.BadMush) == 0 &&
                    Roll(day, salt + 59u, 1000u) < (uint)SoulTune.NatureBadMushPerMille)
                {
                    c.m_Flags2 |= SoulNatureFlags2.BadMush;
                    SoulFeel.Note(em, e, day, SoulEventKind.BadMushroom, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.NatureBadMushMood);
                }
            }

            // F. the strays (F114): feeding turns into naming, naming turns
            //    into a rescue, and the rescue glows up.
            if (age >= (uint)SoulTune.NatureMinAgeDays && (c.m_Flags & SoulNatureFlags.CatFriend) == 0 &&
                (agreeableness >= SoulTune.NatureWarmGateMin || Roll(day, salt + 61u, 100u) < 30u) &&
                (day + salt) % 45u == 0u && Roll(day, salt + 67u, 1000u) < (uint)SoulTune.NatureCatFormPerMille)
            {
                c.m_Flags |= SoulNatureFlags.CatFriend;
                SoulFeel.Note(em, e, day, SoulEventKind.CatFeeder, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.NatureCatMood);
            }
            if ((c.m_Flags & SoulNatureFlags.CatFriend) != 0 && (day + salt) % 21u == 0u)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.CatFeeder, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.NatureCatMood);
                if ((c.m_Flags & SoulNatureFlags.Adopted) == 0 &&
                    Roll(day, salt + 71u, 1000u) < (uint)SoulTune.NatureAdoptPerMille)
                {
                    c.m_Flags |= SoulNatureFlags.Adopted;
                    SoulFeel.Note(em, e, day, SoulEventKind.Adopted, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.NatureAdoptMood);
                }
            }
            if ((c.m_Flags & SoulNatureFlags.Adopted) != 0 && (c.m_Flags2 & SoulNatureFlags2.GlowDone) == 0 &&
                (day + salt) % 60u == 0u)
            {
                c.m_Flags2 |= SoulNatureFlags2.GlowDone;
                SoulFeel.Note(em, e, day, SoulEventKind.GlowUp, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.NatureGlowMood);
            }

            // G. the sky (F115): first snow, the heat, the gray, the storm.
            if (doy == (uint)SoulTune.NatureSnowDay && age >= (uint)SoulTune.NatureMinAgeDays)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.FirstSnow, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.NatureSnowMood);
            }
            if (doy == (uint)SoulTune.NatureHeatDay && age >= (uint)SoulTune.NatureMinAgeDays)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.HeatWave, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.NatureHeatMood);
            }
            if (doy == (uint)SoulTune.NatureGrayDay && age >= (uint)SoulTune.NatureMinAgeDays)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.GrayStreak, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.NatureGrayMood);
            }
            if (doy == (uint)SoulTune.NatureStormDay && age >= (uint)SoulTune.NatureMinAgeDays)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.StormWorry, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.NatureStormMood);
            }

            // H. the planting drive & the old tree (F116): everyone plants;
            //    the grown ones sometimes fight -- and sometimes win.
            if (doy == (uint)SoulTune.NatureDriveDay && age >= (uint)SoulTune.NatureMinAgeDays)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.PlantDrive, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.NatureDriveMood);
            }
            if (age >= (uint)SoulTune.NatureAdultDays && (c.m_Flags & SoulNatureFlags.TreeAngel) == 0 &&
                (day + salt) % 180u == 0u && Roll(day, salt + 73u, 1000u) < (uint)SoulTune.NatureTreeFightPerMille)
            {
                c.m_Flags |= SoulNatureFlags.TreeAngel;
                if (Roll(day, salt + 79u, 1000u) < (uint)SoulTune.NatureTreeSavePerMille)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.TreeSaved, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.NatureTreeMood);
                }
                else
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.TreeFight, 1, e);
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
