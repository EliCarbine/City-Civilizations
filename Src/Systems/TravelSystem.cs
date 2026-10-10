// Citizen Soul -- TravelSystem (M5k): the road out and the road back (F127-F134).
// Vacations are counted down, packed for, and come home with photos and
// the blues. Weekends belong to somewhere close. First keys unlock the
// wash ritual and, for some, the rage. Young souls miss somewhere else
// and call it. Newcomers get a tour or a cold shoulder. A guest from far
// away writes for years after. And the old ones go back once, if at all,
// to remember when all of this was a field.
using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F127-F134: one pass per citizen per day. Travel is the slow lane:
    /// a trip a year, a wave a season, a letter a while.
    /// </summary>
    public partial class TravelSystem : SoulSystemBase
    {
        private const int TravelLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        /// <summary>128 lanes -> 1 pass/citizen/day.</summary>
        protected override int Lanes { get { return TravelLanes; } }

        private EntityQuery m_Travel;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Travel = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulTravel>());
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
                            if (em.HasComponent<SoulTravel>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulTravel.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("travel.migrate." + e.Index, ex);
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
                SoulLog.Once("travel.migrate", "TravelSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Travel.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Travel.ToEntityArray(Allocator.Temp);
                    int faults = 0;
                    try
                    {
                        for (int i = 0; i < souls.Length; i++)
                        {
                            Entity e = souls[i];
                            if (!em.Exists(e)) continue;
                            if (!Matches(e, lane, TravelLanes)) continue;
                            try
                            {
                                Advance(em, e, day);
                            }
                            catch (Exception ex)
                            {
                                faults++;
                                if (faults <= FaultLogCap) SoulLog.Exception("travel.fault." + e.Index, ex);
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
                SoulLog.Exception("travel.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulTravel>(e)) return;
            SoulTravel c = em.GetComponentData<SoulTravel>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            uint age = day >= meta.m_BirthDay ? day - meta.m_BirthDay : 0u;
            uint salt = (uint)e.Index * 2654435761u;

            byte openness = 50;
            byte conscientiousness = 50;
            byte neuroticism = 50;
            if (em.HasComponent<SoulPersonality>(e))
            {
                SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
                openness = p.m_Openness;
                conscientiousness = p.m_Conscientiousness;
                neuroticism = p.m_Neuroticism;
            }

            byte level = 0;
            uint tenure = uint.MaxValue;
            if (em.HasComponent<SoulCareer>(e))
            {
                SoulCareer career = em.GetComponentData<SoulCareer>(e);
                level = career.m_Level;
                tenure = day >= career.m_SinceDay ? day - career.m_SinceDay : 0u;
            }

            // A. vacations (F127): the good one comes in three acts --
            //    counting down, going, and coming home to the blues.
            if (c.m_TripStage == 0)
            {
                uint gap = (uint)SoulTune.TravelTripGapDays + Roll(day, salt + 311u, 90u);
                if (age >= (uint)SoulTune.TravelAdultAgeDays && day >= c.m_LastTripDay + gap)
                {
                    byte tier = 0;
                    if (level >= 3) tier = 2;
                    else if (level >= 1) tier = 1;
                    c.m_TripTier = tier;
                    c.m_TripStage = 1;
                    c.m_TripDay = day + (uint)SoulTune.TravelPlanLeadMin + Roll(day, salt + 313u, (uint)SoulTune.TravelPlanLeadSpan);
                    c.m_Flags |= SoulTravelFlags.Planner;
                    SoulFeel.Note(em, e, day, SoulEventKind.TripPlanOn, (short)(tier + 1), e);
                    SoulFeel.Pulse(em, e, SoulTune.TravelPlanMood);
                }
            }
            else if (c.m_TripStage == 1 && day >= c.m_TripDay)
            {
                c.m_TripStage = 2;
                c.m_TripDay = day + (uint)SoulTune.TravelTripLengthMin + Roll(day, salt + 317u, (uint)SoulTune.TravelTripLengthSpan);
                c.m_Flags |= SoulTravelFlags.Traveler;
                if (c.m_TripTier >= 2) c.m_Flags2 |= SoulTravelFlags2.Abroad;
                SoulFeel.Note(em, e, day, SoulEventKind.TripGoOn, (short)(c.m_TripTier + 1), e);
                SoulFeel.Pulse(em, e, SoulTune.TravelGoMood);
            }
            else if (c.m_TripStage == 2 && day >= c.m_TripDay)
            {
                c.m_TripStage = 0;
                c.m_LastTripDay = day;
                Entity pal = PartnerOrFriend(em, e);
                if (pal != Entity.Null)
                {
                    BumpBoth(em, e, pal, SoulTune.TravelPhotoBump);
                    SoulFeel.Note(em, e, day, SoulEventKind.TripBackOn, (short)(c.m_TripTier + 1), pal);
                }
                else
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.TripBackOn, (short)(c.m_TripTier + 1), e);
                }
                SoulFeel.Pulse(em, e, SoulTune.TravelBackMood);
            }

            // B. day trips (F128): weekends belong to somewhere close --
            //    and every schoolkid remembers the bus ride.
            if (age >= (uint)SoulTune.TravelAdultAgeDays && (day + salt) % 45u == 0u &&
                Roll(day, salt + 331u, 1000u) < (uint)SoulTune.TravelDayTripPerMille)
            {
                c.m_Flags |= SoulTravelFlags.DayTripper;
                Entity pal = PartnerOrFriend(em, e);
                if (pal != Entity.Null)
                {
                    BumpBoth(em, e, pal, SoulTune.TravelDayTripBump);
                    SoulFeel.Note(em, e, day, SoulEventKind.DayTripOn, 1, pal);
                }
                else
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.DayTripOn, 1, e);
                }
                SoulFeel.Pulse(em, e, SoulTune.TravelDayTripMood);
            }
            if (age >= 8u && age < (uint)SoulTune.TravelAdultAgeDays && (day + salt) % 60u == 0u &&
                Roll(day, salt + 337u, 1000u) < (uint)SoulTune.TravelFieldTripPerMille)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.FieldTripOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.TravelFieldTripMood);
            }

            // C. roads (F129): first keys, the wash ritual, the rage.
            if ((c.m_Flags & SoulTravelFlags.FirstCar) == 0 && age >= (uint)SoulTune.TravelAdultAgeDays &&
                (day + salt) % 30u == 0u && Roll(day, salt + 349u, 1000u) < (uint)SoulTune.TravelCarPerMille)
            {
                c.m_Flags |= SoulTravelFlags.FirstCar;
                SoulFeel.Note(em, e, day, SoulEventKind.FirstCarOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.TravelFirstCarMood);
                if (conscientiousness >= 60 && openness <= 45 &&
                    Roll(day, salt + 353u, 1000u) < (uint)SoulTune.TravelCarPersonPerMille)
                {
                    c.m_Flags |= SoulTravelFlags.CarPerson;
                }
            }
            if ((c.m_Flags & SoulTravelFlags.CarPerson) != 0 && (day + salt) % 7u == 0u)
            {
                SoulFeel.Pulse(em, e, SoulTune.TravelSundayMood);
                if (Roll(day, salt + 359u, 1000u) < (uint)SoulTune.TravelCarWashLogPerMille)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.CarWashOn, 1, e);
                }
                else if (Roll(day, salt + 361u, 1000u) < (uint)SoulTune.TravelSundayDrivePerMille)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.SundayDriveOn, 1, e);
                }
            }
            if ((c.m_Flags & SoulTravelFlags.FirstCar) != 0 && neuroticism >= 65 &&
                (day + salt) % 90u == 0u && Roll(day, salt + 367u, 1000u) < (uint)SoulTune.TravelRagePerMille)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.RoadRageOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.TravelRageMood);
            }

            // D. homesickness (F130): the wave comes, and a call rides it out.
            if (age >= (uint)SoulTune.TravelAdultAgeDays && (c.m_Flags & SoulTravelFlags.Homesick) == 0 &&
                (day + salt) % 120u == 0u && Roll(day, salt + 373u, 1000u) < (uint)SoulTune.TravelHomesickPerMille)
            {
                c.m_Flags |= SoulTravelFlags.Homesick;
                SoulFeel.Note(em, e, day, SoulEventKind.MissHomeOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.TravelHomesickMood);
            }
            if ((c.m_Flags & SoulTravelFlags.Homesick) != 0 &&
                (day + salt) % 30u == 0u && Roll(day, salt + 379u, 1000u) < (uint)SoulTune.TravelCallPerMille)
            {
                c.m_Flags = (byte)(c.m_Flags & ~SoulTravelFlags.Homesick);
                SoulFeel.Note(em, e, day, SoulEventKind.CalledHomeOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.TravelCallMood);
            }
            if (age >= (uint)SoulTune.TravelLetterAgeDays &&
                (day + salt) % 180u == 0u && Roll(day, salt + 383u, 1000u) < (uint)SoulTune.TravelLetterPerMille)
            {
                c.m_Flags |= SoulTravelFlags.LetterHome;
                SoulFeel.Note(em, e, day, SoulEventKind.OldLetterOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.TravelLetterMood);
            }

            // E. arrivals (F131): someone shows them around, or nobody does.
            if (tenure == (uint)SoulTune.TravelWelcomeAgeDays && age >= (uint)SoulTune.TravelAdultAgeDays &&
                (c.m_Flags2 & (SoulTravelFlags2.Welcomed | SoulTravelFlags2.ColdShoulder)) == 0)
            {
                uint w = Roll(day, salt + 389u, 1000u);
                if (w < (uint)SoulTune.TravelWelcomeBelow)
                {
                    c.m_Flags2 |= SoulTravelFlags2.Welcomed;
                    SoulFeel.Note(em, e, day, SoulEventKind.WelcomedOn, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.TravelWelcomeMood);
                }
                else if (w < (uint)SoulTune.TravelColdBelow)
                {
                    c.m_Flags2 |= SoulTravelFlags2.ColdShoulder;
                    SoulFeel.Note(em, e, day, SoulEventKind.ColdShoulderOn, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.TravelColdMood);
                }
            }

            // F. threads abroad (F132): a guest from far away, and the mail after.
            if ((c.m_Flags2 & SoulTravelFlags2.Host) == 0 && age >= (uint)SoulTune.TravelAdultAgeDays &&
                openness >= (byte)SoulTune.TravelHostOpenGateMin &&
                (day + salt) % 360u == 0u && Roll(day, salt + 397u, 1000u) < (uint)SoulTune.TravelHostPerMille)
            {
                c.m_Flags2 |= SoulTravelFlags2.Host;
                SoulFeel.Note(em, e, day, SoulEventKind.HostStudentOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.TravelHostMood);
                Entity guestPal = FindFriend(em, e, SoulTune.CurioPartnerMinAffinity);
                if (guestPal != Entity.Null)
                {
                    BumpBoth(em, e, guestPal, SoulTune.TravelHostBump);
                }
            }
            if ((c.m_Flags2 & SoulTravelFlags2.Host) != 0 &&
                (day + salt) % 240u == 0u && Roll(day, salt + 401u, 1000u) < (uint)SoulTune.TravelOverseasLetterPerMille)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.OverseasLetterOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.TravelOverseasLetterMood);
            }

            // G. pilgrimage (F133): the long trip back to where the family is from.
            if ((c.m_Flags & SoulTravelFlags.Pilgrim) == 0 && age >= (uint)SoulTune.TravelPilgrimAgeDays &&
                (day + salt) % 300u == 0u && Roll(day, salt + 409u, 1000u) < (uint)SoulTune.TravelPilgrimPerMille)
            {
                c.m_Flags |= SoulTravelFlags.Pilgrim;
                SoulFeel.Note(em, e, day, SoulEventKind.PilgrimOn, 8, e);
                SoulFeel.Pulse(em, e, SoulTune.TravelPilgrimMood);
            }

            // H. hometown (F134): the remembering, and who does it longest.
            if (age >= (uint)SoulTune.TravelNostalgiaAgeDays &&
                (day + salt) % 270u == 0u && Roll(day, salt + 419u, 1000u) < (uint)SoulTune.TravelNostalgiaPerMille)
            {
                c.m_Flags2 |= SoulTravelFlags2.Nostalgic;
                if (tenure >= (uint)SoulTune.TravelLiferTenureDays)
                {
                    c.m_Flags2 |= SoulTravelFlags2.Lifer;
                }
                SoulFeel.Note(em, e, day, SoulEventKind.RememberWhenOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.TravelNostalgiaMood);
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
