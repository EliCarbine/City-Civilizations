using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// M3 (F31–F33): street life — desperation theft, sour vandalism, gang recruitment.
    /// Heat (notoriety) accrues with each act and draws the law in JusticeSystem.
    /// </summary>
    public partial class StreetSystem : SoulSystemBase
    {
        protected override int Interval { get { return 4096; } }
        protected override int Lanes { get { return SoulConsts.StreetLanes; } }

        private EntityQuery m_Souls;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Souls = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<SoulMeta>() }
            });
        }

        protected override void OnSoulUpdate()
        {
            uint day; int lane;
            if (!AdvanceDay(out day, out lane)) return;

            EntityManager em = EntityManager;
            NativeArray<Entity> souls = m_Souls.ToEntityArray(Allocator.Temp);
            int faults = 0;
            try
            {
                for (int i = 0; i < souls.Length; i++)
                {
                    Entity e = souls[i];
                    try
                    {
                        if (!Matches(e, lane, Lanes)) continue;
                        if (!em.HasComponent<SoulMeta>(e)) continue;
                        if (!em.HasComponent<Mood>(e) || !em.HasComponent<SoulPersonality>(e)) continue;
                        if (!em.HasComponent<SoulCrime>(e)) em.AddComponentData(e, default(SoulCrime));
                        Tick(em, e, day);
                    }
                    catch (Exception ex)
                    {
                        faults++;
                        SoulLog.Exception("street.tick", ex);
                    }
                }
            }
            finally
            {
                souls.Dispose();
            }
            if (faults > 0)
            {
                SoulLog.Throttled("street.faults", faults + " street fault(s) this slice", 60000);
            }
        }

        private static void Tick(EntityManager em, Entity e, uint day)
        {
            SoulCrime crime = em.GetComponentData<SoulCrime>(e);
            if (crime.m_StreetDay == day) return;
            crime.m_StreetDay = day;

            Mood mood = em.GetComponentData<Mood>(e);
            SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
            int valence = mood.m_Valence;
            uint salt = (uint)e.Index * 2654435761u;

            // ---- F31/F32: sour or desperate souls take to the streets ----
            int urge = 0;
            bool theft = false;
            if (valence <= SoulTune.DesperateMood)
            {
                urge = SoulTune.TheftPerMille;                       // F31: desperation lifts goods
                theft = true;
            }
            else if (valence <= SoulTune.SourMood && p.m_Conscientiousness < 140)
            {
                urge = SoulTune.VandalPerMille;                      // F32: sour taggers mark walls
            }
            if (crime.m_Gang > 0) urge += crime.m_Gang * SoulTune.GangCrimeBonusPerGang;

            if (urge > 0 && Roll(day, salt + 81u, 1000u) < (uint)urge)
            {
                crime.m_LastCrimeDay = day;
                bool caught = Roll(day, salt + 82u, 1000u) < SoulTune.CrimeCaughtPerMille;
                SoulFeel.Note(em, e, day, theft ? SoulEventKind.Theft : SoulEventKind.Vandalism,
                              (short)(caught ? 1 : 0), Entity.Null);
                if (caught)
                {
                    SoulFeel.Pulse(em, e, SoulTune.CrimeFineMood);   // fine + record
                    crime.m_Notoriety = (byte)Math.Min(255, crime.m_Notoriety + (theft ? 3 : 2));
                }
                else
                {
                    crime.m_Notoriety = (byte)Math.Min(255, crime.m_Notoriety + 1);
                }
            }

            // ---- F33: recruiters notice disaffected souls with heat ----
            if (crime.m_Gang == 0 && valence <= SoulTune.SourMood &&
                crime.m_Notoriety >= SoulTune.GangJoinAtNotoriety &&
                Roll(day, salt + 83u, 1000u) < SoulTune.GangJoinPerMille)
            {
                crime.m_Gang = (byte)(1 + Roll(day, salt + 84u, 3u));
                SoulFeel.Note(em, e, day, SoulEventKind.GangJoined, (short)crime.m_Gang, Entity.Null);
                SoulFeel.Pulse(em, e, SoulTune.GangMood);            // belonging, paid for
            }

            em.SetComponentData(e, crime);
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

    /// <summary>
    /// M3 (F35): arrests, trial &amp; prison — heat draws the law, the gavel weighs the
    /// record, and time is served off the streets. Release is a reunion with freedom.
    /// </summary>
    public partial class JusticeSystem : SoulSystemBase
    {
        protected override int Interval { get { return 4096; } }
        protected override int Lanes { get { return SoulConsts.JusticeLanes; } }

        private EntityQuery m_Souls;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Souls = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<SoulMeta>() }
            });
        }

        protected override void OnSoulUpdate()
        {
            uint day; int lane;
            if (!AdvanceDay(out day, out lane)) return;

            EntityManager em = EntityManager;
            NativeArray<Entity> souls = m_Souls.ToEntityArray(Allocator.Temp);
            int faults = 0;
            try
            {
                for (int i = 0; i < souls.Length; i++)
                {
                    Entity e = souls[i];
                    try
                    {
                        if (!Matches(e, lane, Lanes)) continue;
                        if (!em.HasComponent<SoulMeta>(e)) continue;
                        if (!em.HasComponent<SoulCrime>(e)) em.AddComponentData(e, default(SoulCrime));
                        Tick(em, e, day);
                    }
                    catch (Exception ex)
                    {
                        faults++;
                        SoulLog.Exception("justice.tick", ex);
                    }
                }
            }
            finally
            {
                souls.Dispose();
            }
            if (faults > 0)
            {
                SoulLog.Throttled("justice.faults", faults + " justice fault(s) this slice", 60000);
            }
        }

        private static void Tick(EntityManager em, Entity e, uint day)
        {
            SoulCrime crime = em.GetComponentData<SoulCrime>(e);
            if (crime.m_JusticeDay == day) return;
            crime.m_JusticeDay = day;
            uint salt = (uint)e.Index * 2654435761u;

            if (em.HasComponent<Imprisoned>(e) && em.IsComponentEnabled<Imprisoned>(e))
            {
                Imprisoned imp = em.GetComponentData<Imprisoned>(e);
                if (day >= imp.m_UntilDay)
                {
                    // Released: the streets changed, and so did they.
                    em.SetComponentEnabled<Imprisoned>(e, false);
                    crime.m_Notoriety = (byte)Math.Max(0, crime.m_Notoriety - SoulTune.ReleaseNotorietyRelief);
                    SoulFeel.Note(em, e, day, SoulEventKind.Released, (short)crime.m_Records, Entity.Null);
                    SoulFeel.Pulse(em, e, SoulTune.ReleaseMood);
                }
                else
                {
                    SoulFeel.Pulse(em, e, SoulTune.PrisonMoodDrag);
                    if (Roll(day, salt + 91u, 1000u) < SoulTune.PrisonTraumaPerMille && em.HasComponent<SoulMind>(e))
                    {
                        SoulMind mind = em.GetComponentData<SoulMind>(e);
                        mind.m_Trauma = (byte)Math.Min(255, mind.m_Trauma + SoulTune.PrisonTrauma);
                        mind.m_ScarDay = day;
                        em.SetComponentData(e, mind);
                    }
                }
            }
            else
            {
                // F35: heat cools on clean days.
                if (day - crime.m_LastDecayDay >= (uint)SoulTune.NotorietyDecayDays)
                {
                    crime.m_LastDecayDay = day;
                    if (crime.m_Notoriety > 0) crime.m_Notoriety--;
                }

                // ---- arrest & trial ----
                if (crime.m_Notoriety >= SoulTune.ArrestAtNotoriety)
                {
                    uint heat = (uint)(SoulTune.ArrestPerMille + crime.m_Notoriety * SoulTune.ArrestPerNotoriety);
                    // F35: recently-injured souls carry extra heat — bad fight outcomes draw the law.
                    if (em.HasComponent<Injured>(e) && em.IsComponentEnabled<Injured>(e)) heat += 10u;

                    if (Roll(day, salt + 92u, 1000u) < heat)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.Arrested, (short)crime.m_Notoriety, Entity.Null);
                        int convictPermille = Math.Min(SoulTune.ConvictCapPerMille,
                            SoulTune.ConvictBasePerMille + crime.m_Notoriety * SoulTune.ConvictPerNotoriety);
                        if (Roll(day, salt + 93u, 1000u) < (uint)convictPermille)
                        {
                            crime.m_Records = (byte)Math.Min(255, crime.m_Records + 1);
                            int sentence = SoulTune.PrisonDaysPerStrike * crime.m_Records;
                            if (em.HasComponent<Imprisoned>(e))
                            {
                                Imprisoned imp = em.GetComponentData<Imprisoned>(e);
                                imp.m_SinceDay = day;
                                imp.m_UntilDay = day + (uint)sentence;
                                em.SetComponentData(e, imp);
                                em.SetComponentEnabled<Imprisoned>(e, true);
                            }
                            else
                            {
                                Imprisoned imp = default(Imprisoned);
                                imp.m_SinceDay = day;
                                imp.m_UntilDay = day + (uint)sentence;
                                em.AddComponentData(e, imp);
                            }
                            SoulFeel.Note(em, e, day, SoulEventKind.TrialConvicted,
                                          (short)Math.Min(sentence, 32000), Entity.Null);
                        }
                        else
                        {
                            crime.m_Notoriety = (byte)Math.Max(0, crime.m_Notoriety - SoulTune.AcquitNotorietyRelief);
                            SoulFeel.Note(em, e, day, SoulEventKind.TrialAcquitted, (short)crime.m_Notoriety, Entity.Null);
                            SoulFeel.Pulse(em, e, 4);   // walked, barely
                        }
                    }
                }
            }

            em.SetComponentData(e, crime);
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
