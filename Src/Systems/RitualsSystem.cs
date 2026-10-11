// Citizen Soul -- RitualsSystem (M5m): the ceremonies of a life (F142-F151).
// One pass per citizen per day: the driving test, the first apartment and
// its noodle era, the graduation walk, the paycheck and the plaque, dinner
// in one's own place, renovations, gardens, resolutions, founding days,
// send-offs, reunions, and coming-of-age. Extends F129 (TravelSystem holds
// the keys once the test is passed) and F117/F124 (QuirksSystem keeps the
// private rituals; these are the ones the street watches).
using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F142-F151: rituals and milestones. Every citizen is visited on their
    /// stagger lane; each beat fires at most once unless a flag is cleared
    /// (a failed driving test resets lessons). Everything is journaled via
    /// SoulFeel.Note so the story writer can quote it back years later.
    /// </summary>
    public partial class RitualsSystem : SoulSystemBase
    {
        private const int RitualLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        /// <summary>128 lanes -> 1 pass/citizen/day.</summary>
        protected override int Lanes { get { return RitualLanes; } }

        private EntityQuery m_Rituals;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Rituals = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulRitual>());
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
                            if (em.HasComponent<SoulRitual>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulRitual.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("ritual.migrate." + e.Index, ex);
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
                SoulLog.Once("ritual.migrate", "RitualsSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Rituals.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Rituals.ToEntityArray(Allocator.Temp);
                    int faults = 0;
                    try
                    {
                        for (int i = 0; i < souls.Length; i++)
                        {
                            Entity e = souls[i];
                            if (!em.Exists(e)) continue;
                            if (!Matches(e, lane, RitualLanes)) continue;
                            try
                            {
                                Advance(em, e, day);
                            }
                            catch (Exception ex)
                            {
                                faults++;
                                if (faults <= FaultLogCap) SoulLog.Exception("ritual.fault." + e.Index, ex);
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
                SoulLog.Exception("ritual.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulRitual>(e)) return;
            SoulRitual c = em.GetComponentData<SoulRitual>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            uint age = day >= meta.m_BirthDay ? day - meta.m_BirthDay : 0u;
            uint salt = (uint)e.Index * 2654435761u;

            byte conscientiousness = 50;
            byte neuroticism = 50;
            byte openness = 50;
            if (em.HasComponent<SoulPersonality>(e))
            {
                SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
                conscientiousness = p.m_Conscientiousness;
                neuroticism = p.m_Neuroticism;
                openness = p.m_Openness;
            }

            bool teen = age >= (uint)SoulTune.RitualRiteMinAgeDays;
            bool driver = age >= (uint)SoulTune.RitualEdMinAgeDays;
            bool adult = age >= (uint)SoulTune.RitualGraduateAgeDays;
            bool senior = age >= (uint)SoulTune.RitualSeniorAgeDays;

            // A. F142 -- driver's ed to license; F129 takes over from there.
            if (driver && (c.m_Flags & SoulRitualFlags.DriversEd) == 0 &&
                (day + salt) % 15u == 0u &&
                Roll(day, salt + 601u, 1000u) < (uint)SoulTune.RitualEdPerMille)
            {
                c.m_Flags |= SoulRitualFlags.DriversEd;
                c.m_CarDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.DriversEdOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.RitualEdMood);
            }
            if ((c.m_Flags & SoulRitualFlags.DriversEd) != 0 && (c.m_Flags & SoulRitualFlags.Licensed) == 0 &&
                (day + salt) % 14u == 0u &&
                Roll(day, salt + 602u, 1000u) < (uint)SoulTune.RitualEdPracticePerMille)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.DriversEdOn, 2, e);
                SoulFeel.Pulse(em, e, SoulTune.RitualEdMood);
            }
            if ((c.m_Flags & SoulRitualFlags.DriversEd) != 0 && (c.m_Flags & SoulRitualFlags.Licensed) == 0 &&
                day >= c.m_CarDay + (uint)SoulTune.RitualTestDays)
            {
                int pass = SoulTune.RitualPassBasePerMille + conscientiousness * 3;
                if (pass > 950) pass = 950;
                if (Roll(day, salt + 603u, 1000u) < (uint)pass)
                {
                    c.m_Flags |= SoulRitualFlags.Licensed;
                    SoulFeel.Note(em, e, day, SoulEventKind.LicenseOn, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.RitualLicenseMood);
                }
                else
                {
                    c.m_CarDay = day;
                    SoulFeel.Note(em, e, day, SoulEventKind.LicenseOn, 0, e);
                    SoulFeel.Pulse(em, e, SoulTune.RitualFailMood);
                }
            }

            // B. F143 -- the first apartment, the housewarming, the noodle era.
            if (adult && (c.m_Flags & SoulRitualFlags.Apartment) == 0 &&
                (day + salt) % 30u == 0u &&
                Roll(day, salt + 611u, 1000u) < (uint)SoulTune.RitualApartmentPerMille)
            {
                c.m_Flags |= SoulRitualFlags.Apartment;
                c.m_ApartmentDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.MovingDayOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.RitualApartmentMood);
            }
            if ((c.m_Flags & SoulRitualFlags.Apartment) != 0 && (c.m_Flags & SoulRitualFlags.Housewarming) == 0 &&
                day >= c.m_ApartmentDay + (uint)SoulTune.RitualWarmDays)
            {
                c.m_Flags |= SoulRitualFlags.Housewarming;
                Entity pal = FindFriend(em, e, SoulTune.RitualWarmMinAffinity);
                SoulFeel.Note(em, e, day, SoulEventKind.HousewarmingOn, 1, pal == Entity.Null ? e : pal);
                SoulFeel.Pulse(em, e, SoulTune.RitualWarmMood);
                if (pal != Entity.Null) BumpBoth(em, e, pal, SoulTune.RitualWarmBump);
            }
            if ((c.m_Flags & SoulRitualFlags.Apartment) != 0 && c.m_ApartmentDay != 0u &&
                day > c.m_ApartmentDay && (day - c.m_ApartmentDay) % 365u == 0u)
            {
                short years = (short)((day - c.m_ApartmentDay) / 365u);
                SoulFeel.Note(em, e, day, SoulEventKind.NoodleEraOn, years, e);
                SoulFeel.Pulse(em, e, SoulTune.RitualNoodleMood);
            }

            // C. F144 -- school: the near-miss, the walk, and the dinner after.
            if (teen && !adult && (c.m_Flags & SoulRitualFlags.NearDropout) == 0 &&
                (day + salt) % 45u == 0u &&
                Roll(day, salt + 621u, 1000u) < (uint)SoulTune.RitualDropoutPerMille)
            {
                c.m_Flags |= SoulRitualFlags.NearDropout;
                SoulFeel.Note(em, e, day, SoulEventKind.NearDropoutOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.RitualDropoutMood);
            }
            if (adult && (c.m_Flags & SoulRitualFlags.Graduated) == 0 &&
                (day + salt) % 20u == 0u &&
                Roll(day, salt + 622u, 1000u) < (uint)SoulTune.RitualGradPerMille)
            {
                c.m_Flags |= SoulRitualFlags.Graduated;
                c.m_GradDay = day;
                bool almost = (c.m_Flags & SoulRitualFlags.NearDropout) != 0;
                SoulFeel.Note(em, e, day, SoulEventKind.GraduationOn, almost ? (short)1 : (short)0, e);
                SoulFeel.Pulse(em, e, SoulTune.RitualGradMood);
            }
            if ((c.m_Flags & SoulRitualFlags.Graduated) != 0 && (c.m_Flags & SoulRitualFlags.PrideWave) == 0 &&
                day >= c.m_GradDay + (uint)SoulTune.RitualPrideDays)
            {
                c.m_Flags |= SoulRitualFlags.PrideWave;
                Entity pal = FindFriend(em, e, SoulTune.RitualPalMinAffinity);
                SoulFeel.Note(em, e, day, SoulEventKind.PrideWaveOn, 1, pal == Entity.Null ? e : pal);
                SoulFeel.Pulse(em, e, SoulTune.RitualPrideMood);
                if (pal != Entity.Null) BumpBoth(em, e, pal, SoulTune.RitualPrideBump);
            }

            // ---- F145: the working life (first pay, toast, watch, plaque) ----
            if ((c.m_Flags & SoulRitualFlags.Graduated) != 0 && (c.m_Flags & SoulRitualFlags.FirstPay) == 0 &&
                day >= c.m_GradDay + (uint)SoulTune.RitualPayDays)
            {
                c.m_Flags = (byte)(c.m_Flags | SoulRitualFlags.FirstPay);
                c.m_JobDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.FirstPaycheckOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.RitualPayMood);
            }

            if (em.HasComponent<SoulCareer>(e))
            {
                SoulCareer career = em.GetComponentData<SoulCareer>(e);
                if ((c.m_Flags2 & SoulRitualFlags2.Toast) == 0 && career.m_Level >= (byte)SoulTune.RitualToastGate)
                {
                    c.m_Flags2 = (byte)(c.m_Flags2 | SoulRitualFlags2.Toast);
                    SoulFeel.Note(em, e, day, SoulEventKind.PromotionToastOn, (short)career.m_Level, e);
                    SoulFeel.Pulse(em, e, SoulTune.RitualToastMood);
                }

                if (senior && (c.m_Flags2 & SoulRitualFlags2.Retired) == 0 && c.m_JobDay != 0u &&
                    day >= c.m_JobDay + (uint)(SoulTune.RitualWatchTenureYears * 365) &&
                    Roll(day, salt + 660u, 1000u) < (uint)SoulTune.RitualWatchPerMille)
                {
                    c.m_Flags2 = (byte)(c.m_Flags2 | SoulRitualFlags2.Retired);
                    SoulFeel.Note(em, e, day, SoulEventKind.RetirementWatchOn, (short)career.m_Level, e);
                    SoulFeel.Pulse(em, e, SoulTune.RitualWatchMood);
                }
            }

            if ((c.m_Flags2 & SoulRitualFlags2.Plaque) == 0 && c.m_JobDay != 0u &&
                day >= c.m_JobDay + (uint)(SoulTune.RitualPlaqueYears * 365))
            {
                c.m_Flags2 = (byte)(c.m_Flags2 | SoulRitualFlags2.Plaque);
                Entity crew = FindFriend(em, e, SoulTune.RitualPalMinAffinity);
                SoulFeel.Note(em, e, day, SoulEventKind.PlaquePartyOn, (short)SoulTune.RitualPlaqueYears, crew == Entity.Null ? e : crew);
                SoulFeel.Pulse(em, e, SoulTune.RitualPlaqueMood);
                if (crew != Entity.Null) BumpBoth(em, e, crew, SoulTune.RitualPrideBump);
            }

            // ---- F146: home making (first dinner, renovation, first bloom) ----
            if ((c.m_Flags & SoulRitualFlags.Apartment) != 0 && (c.m_Flags2 & SoulRitualFlags2.FirstDinner) == 0 &&
                day >= c.m_ApartmentDay + (uint)SoulTune.RitualDinnerDays)
            {
                c.m_Flags2 = (byte)(c.m_Flags2 | SoulRitualFlags2.FirstDinner);
                Entity mate = PartnerOrFriend(em, e);
                SoulFeel.Note(em, e, day, SoulEventKind.FirstDinnerOn, 1, mate == Entity.Null ? e : mate);
                SoulFeel.Pulse(em, e, SoulTune.RitualDinnerMood);
                if (mate != Entity.Null) BumpBoth(em, e, mate, SoulTune.RitualWarmBump);
            }

            if ((c.m_Flags2 & SoulRitualFlags2.FirstDinner) != 0 && (c.m_Flags2 & SoulRitualFlags2.Renovating) == 0 &&
                (c.m_Flags2 & SoulRitualFlags2.Renovated) == 0 &&
                Roll(day, salt + 631u, 1000u) < (uint)SoulTune.RitualRenoPerMille)
            {
                c.m_Flags2 = (byte)(c.m_Flags2 | SoulRitualFlags2.Renovating);
                c.m_RenoDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.RenovationOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.RitualRenoMood);
                if (Roll(day, salt + 632u, 1000u) < (uint)SoulTune.RitualRenoStressPerMille)
                    SoulFeel.Pulse(em, e, SoulTune.RitualRenoStressMood);
            }

            if ((c.m_Flags2 & SoulRitualFlags2.Renovating) != 0 && day >= c.m_RenoDay + (uint)SoulTune.RitualRenoDays)
            {
                c.m_Flags2 = (byte)(c.m_Flags2 & ~SoulRitualFlags2.Renovating);
                c.m_Flags2 = (byte)(c.m_Flags2 | SoulRitualFlags2.Renovated);
                SoulFeel.Note(em, e, day, SoulEventKind.RenoDoneOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.RitualRenoDoneMood);
            }

            if ((c.m_Flags2 & SoulRitualFlags2.Renovated) != 0 && (c.m_Flags2 & SoulRitualFlags2.Bloomed) == 0 &&
                Roll(day, salt + 633u, 1000u) < (uint)SoulTune.RitualBloomPerMille)
            {
                c.m_Flags2 = (byte)(c.m_Flags2 | SoulRitualFlags2.Bloomed);
                SoulFeel.Note(em, e, day, SoulEventKind.FirstBloomOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.RitualBloomMood);
            }

            // ---- F147: resolutions (say it, keep it, show up) ----
            if (adult && day % 365u == 1u && (c.m_Flags2 & SoulRitualFlags2.Resolution) == 0 &&
                Roll(day, salt + 651u, 1000u) < (uint)SoulTune.RitualResolvePerMille)
            {
                c.m_Flags2 = (byte)(c.m_Flags2 | SoulRitualFlags2.Resolution);
                c.m_ResolveDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.ResolutionOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.RitualResolveMood);
            }

            if ((c.m_Flags2 & SoulRitualFlags2.Resolution) != 0 && day >= c.m_ResolveDay + (uint)SoulTune.RitualResolveDays)
            {
                if (Roll(day, salt + 652u, 1000u) < (uint)SoulTune.RitualKeepPerMille)
                {
                    c.m_Flags2 = (byte)(c.m_Flags2 & ~SoulRitualFlags2.Resolution);
                    c.m_Flags3 = (byte)(c.m_Flags3 | SoulRitualFlags3.ResolutionKept);
                    if (c.m_Streak < 100) c.m_Streak++;
                    SoulFeel.Note(em, e, day, SoulEventKind.ResolutionKeptOn, (short)c.m_Streak, e);
                    SoulFeel.Pulse(em, e, SoulTune.RitualKeptMood);
                    Entity buddy = PartnerOrFriend(em, e);
                    if (buddy != Entity.Null) BumpBoth(em, e, buddy, SoulTune.RitualKeptBump);
                }
                else
                {
                    c.m_Flags2 = (byte)(c.m_Flags2 & ~SoulRitualFlags2.Resolution);
                }
            }

            if (adult && (c.m_Flags3 & SoulRitualFlags3.ResolutionKept) != 0 && day % 365u == 45u &&
                Roll(day, salt + 653u, 1000u) < (uint)SoulTune.RitualGymPerMille)
            {
                Entity gymPal = FindFriend(em, e, SoulTune.RitualPalMinAffinity);
                SoulFeel.Note(em, e, day, SoulEventKind.GymCrowdOn, (short)c.m_Streak, gymPal == Entity.Null ? e : gymPal);
                SoulFeel.Pulse(em, e, SoulTune.RitualGymMood);
            }

            // ---- F148: town rituals (founding day, bridge day, local pride) ----
            if (day % 365u == 200u && Roll(day, salt + 641u, 1000u) < (uint)SoulTune.RitualFoundingPerMille)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.FoundingDayOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.RitualFoundingMood);
            }

            if (day % 365u == 300u && Roll(day, salt + 642u, 1000u) < (uint)SoulTune.RitualFoundingPerMille)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.BridgeDayOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.RitualFoundingMood);
            }

            if ((c.m_Flags & SoulRitualFlags.PrideWave) != 0 && day % 365u == 60u &&
                Roll(day, salt + 643u, 1000u) < (uint)SoulTune.RitualStreetPerMille)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.LocalPrideOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.RitualPrideAnnealMood);
                Entity partner = PartnerOrFriend(em, e);
                if (partner != Entity.Null) BumpBoth(em, e, partner, SoulTune.RitualPrideAnnealBump);
            }

            // ---- F149: send-offs (goodbye, the station run, letters) ----
            if (adult && (c.m_Flags3 & SoulRitualFlags3.SentOff) == 0 &&
                Roll(day, salt + 671u, 1000u) < (uint)SoulTune.RitualSendPerMille)
            {
                Entity friend = FindFriend(em, e, SoulTune.RitualCloseMinAffinity);
                if (friend != Entity.Null)
                {
                    c.m_Flags3 = (byte)(c.m_Flags3 | SoulRitualFlags3.SentOff);
                    c.m_SentDay = day;
                    c.m_SendMate = friend;
                    SoulFeel.Note(em, e, day, SoulEventKind.SendOffOn, 1, friend);
                    SoulFeel.Pulse(em, e, SoulTune.RitualSendMood);
                    BumpBoth(em, e, friend, SoulTune.RitualSendBump);
                }
            }

            if ((c.m_Flags3 & SoulRitualFlags3.SentOff) != 0 && (c.m_Flags3 & SoulRitualFlags3.Letters) == 0 &&
                day == c.m_SentDay + (uint)SoulTune.RitualStationDays)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.StationRunOn, 1, c.m_SendMate);
                SoulFeel.Pulse(em, e, SoulTune.RitualStationMood);
            }

            if ((c.m_Flags3 & SoulRitualFlags3.SentOff) != 0 && (c.m_Flags3 & SoulRitualFlags3.Letters) == 0 &&
                day == c.m_SentDay + (uint)SoulTune.RitualLetterDelayDays)
            {
                c.m_Flags3 = (byte)(c.m_Flags3 | SoulRitualFlags3.Letters);
                SoulFeel.Note(em, e, day, SoulEventKind.LetterFollowOn, 1, c.m_SendMate);
                SoulFeel.Pulse(em, e, SoulTune.RitualLetterMood);
            }

            // ---- F150: reunions (the plan, the night, the rekindle) ----
            if (adult && (c.m_Flags3 & SoulRitualFlags3.ReunionPlan) == 0 &&
                (c.m_Flags3 & SoulRitualFlags3.Letters) != 0 &&
                day >= c.m_SentDay + (uint)SoulTune.RitualReunionDelayDays &&
                Roll(day, salt + 681u, 1000u) < (uint)SoulTune.RitualReunionPerMille)
            {
                c.m_Flags3 = (byte)(c.m_Flags3 | SoulRitualFlags3.ReunionPlan);
                c.m_ReunionDay = day + (uint)SoulTune.RitualReunionWaitDays;
                SoulFeel.Note(em, e, day, SoulEventKind.ReunionPlanOn, 1, c.m_SendMate);
                SoulFeel.Pulse(em, e, SoulTune.RitualReunionPlanMood);
            }

            if ((c.m_Flags3 & SoulRitualFlags3.ReunionPlan) != 0 && (c.m_Flags3 & SoulRitualFlags3.ReunionNight) == 0 &&
                day >= c.m_ReunionDay)
            {
                c.m_Flags3 = (byte)(c.m_Flags3 | SoulRitualFlags3.ReunionNight);
                SoulFeel.Note(em, e, day, SoulEventKind.ReunionNightOn, 1, c.m_SendMate);
                SoulFeel.Pulse(em, e, SoulTune.RitualReunionMood);
                BumpBoth(em, e, c.m_SendMate, SoulTune.RitualReunionBump);
            }

            if ((c.m_Flags3 & SoulRitualFlags3.ReunionNight) != 0 && (c.m_Flags3 & SoulRitualFlags3.Rekindled) == 0 &&
                Roll(day, salt + 682u, 1000u) < (uint)SoulTune.RitualRekindlePerMille)
            {
                c.m_Flags3 = (byte)(c.m_Flags3 | SoulRitualFlags3.Rekindled);
                SoulFeel.Note(em, e, day, SoulEventKind.RekindleOn, 1, c.m_SendMate);
                SoulFeel.Pulse(em, e, SoulTune.RitualRekindleMood);
                BumpBoth(em, e, c.m_SendMate, SoulTune.RitualRekindleBump);
            }

            // ---- F151: the rite (prep, the walk, the keeping) ----
            if (teen && (c.m_Flags3 & SoulRitualFlags3.RitePrep) == 0 &&
                Roll(day, salt + 701u, 1000u) < (uint)SoulTune.RitualRitePrepPerMille)
            {
                c.m_Flags3 = (byte)(c.m_Flags3 | SoulRitualFlags3.RitePrep);
                c.m_RiteDay = day;
                c.m_Variant = (byte)(1u + Roll(day, salt + 702u, 3u));
                SoulFeel.Note(em, e, day, SoulEventKind.RitePrepOn, (short)c.m_Variant, e);
                SoulFeel.Pulse(em, e, SoulTune.RitualRitePrepMood);
            }

            if ((c.m_Flags3 & SoulRitualFlags3.RitePrep) != 0 && (c.m_Flags3 & SoulRitualFlags3.RiteDone) == 0 &&
                day >= c.m_RiteDay + (uint)SoulTune.RitualRiteSpanDays)
            {
                c.m_Flags3 = (byte)(c.m_Flags3 | SoulRitualFlags3.RiteDone);
                Entity guide = PartnerOrFriend(em, e);
                SoulFeel.Note(em, e, day, SoulEventKind.RiteDoneOn, (short)c.m_Variant, guide == Entity.Null ? e : guide);
                SoulFeel.Pulse(em, e, SoulTune.RitualRiteMood);
                if (guide != Entity.Null) BumpBoth(em, e, guide, SoulTune.RitualRiteBump);
            }

            if (adult && (c.m_Flags3 & SoulRitualFlags3.RiteDone) != 0 && c.m_Variant >= 1 && c.m_Variant <= 3 &&
                day >= c.m_RiteDay + (uint)SoulTune.RitualRiteSpanDays + 365u)
            {
                c.m_Variant = 4;
                SoulFeel.Note(em, e, day, SoulEventKind.ComingOfAgeOn, 4, e);
                SoulFeel.Pulse(em, e, SoulTune.RitualRiteMood);
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
