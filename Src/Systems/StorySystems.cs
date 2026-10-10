using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// M4 (F43): passings and remembrance. A soul's edge to someone who no longer exists is
    /// an orphan; finding one is grief - a Mourned memory, a mood hit, a scar for the closest
    /// bonds. The registry remembers every passing (bounded ring) and every year the first
    /// witness still marks the anniversary. Index+version dedupes and keeps reuse honest.
    /// </summary>
    public partial class MemorialSystem : SoulSystemBase
    {
        protected override int Interval { get { return 4096; } }
        protected override int Lanes { get { return SoulConsts.MemorialLanes; } }

        private EntityQuery m_Souls;
        private EntityQuery m_Registry;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Souls = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<SoulMeta>() }
            });
            m_Registry = GetEntityQuery(ComponentType.ReadWrite<SoulPassing>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            EntityManager em = EntityManager;

            // Registry singleton - lazy migration for saves that predate M4.
            if (m_Registry.CalculateEntityCount() == 0)
            {
                Entity reg = em.CreateEntity();
                em.AddBuffer<SoulPassing>(reg);
                SoulLog.Info("MemorialSystem: passing registry created.");
            }

            if (lane == 0) SweepAnniversaries(em, day);

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
                        Mourn(em, e, day);
                    }
                    catch (Exception ex)
                    {
                        faults++;
                        SoulLog.Exception("memorial.tick", ex);
                    }
                }
            }
            finally
            {
                souls.Dispose();
            }
            if (faults > 0) SoulLog.Throttled("memorial.faults", faults + " memorial fault(s)", 60000);
        }

        /// <summary>Grieves every orphaned edge a soul still holds; forgets the orphan after.</summary>
        private void Mourn(EntityManager em, Entity e, uint day)
        {
            if (!em.HasBuffer<SoulEdge>(e)) return;
            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
            for (int k = 0; k < edges.Length; k++)
            {
                Entity other = edges[k].m_Other;
                if (other == Entity.Null || em.Exists(other)) continue;

                int affinity = edges[k].m_Affinity;
                edges.RemoveAt(k);
                k--;

                int memories = CountMemories(em, e, other);
                SoulFeel.Note(em, e, day, SoulEventKind.Mourned, (short)affinity, Entity.Null);
                SoulFeel.Pulse(em, e, SoulTune.MournMoodBase - affinity / 16);

                if (affinity >= SoulTune.MournCloseAffinity && em.HasComponent<SoulMind>(e))
                {
                    SoulMind mind = em.GetComponentData<SoulMind>(e);
                    mind.m_Trauma = (byte)Math.Min(255, mind.m_Trauma + SoulTune.MournTrauma);
                    mind.m_ScarDay = day;
                    em.SetComponentData(e, mind);
                }

                Remember(em, day, e, other, memories);
            }
        }

        private void Remember(EntityManager em, uint day, Entity detector, Entity gone, int memories)
        {
            Entity reg = m_Registry.GetSingletonEntity();
            DynamicBuffer<SoulPassing> ring = em.GetBuffer<SoulPassing>(reg);
            for (int i = 0; i < ring.Length; i++)
            {
                if (ring[i].m_Index != gone.Index || ring[i].m_Version != gone.Version) continue;
                SoulPassing known = ring[i];
                if (known.m_Mourners < 255) known.m_Mourners++;
                if (memories > known.m_Memories) known.m_Memories = (byte)Math.Min(memories, 255);
                ring[i] = known;
                return;
            }
            SoulPassing fresh = default(SoulPassing);
            fresh.m_Index = gone.Index;
            fresh.m_Version = gone.Version;
            fresh.m_Detector = detector;
            fresh.m_Day = day;
            fresh.m_Mourners = 1;
            fresh.m_Memories = (byte)Math.Min(memories, 255);
            if (ring.Length >= SoulTune.RegistryCap) ring.RemoveAt(0);
            ring.Add(fresh);
        }

        /// <summary>Every passing's anniversary - the first witness still marks it.</summary>
        private void SweepAnniversaries(EntityManager em, uint day)
        {
            Entity reg = m_Registry.GetSingletonEntity();
            DynamicBuffer<SoulPassing> ring = em.GetBuffer<SoulPassing>(reg);
            for (int i = 0; i < ring.Length; i++)
            {
                SoulPassing p = ring[i];
                if (p.m_Day == 0 || day <= p.m_Day) continue;
                uint age = day - p.m_Day;
                if (age % (uint)SoulTune.YearDays != 0) continue;
                Entity watcher = p.m_Detector;
                if (watcher == Entity.Null || !em.Exists(watcher)) continue;
                if (!em.HasComponent<SoulMeta>(watcher)) continue;
                SoulFeel.Pulse(em, watcher, SoulTune.MemorialAnnivMood);
                SoulFeel.Note(em, watcher, day, SoulEventKind.AnniversaryFlare, (short)Math.Min(age, 32000u), Entity.Null);
            }
        }

        private static int CountMemories(EntityManager em, Entity mourner, Entity subject)
        {
            if (!em.HasBuffer<SoulMemory>(mourner)) return 0;
            DynamicBuffer<SoulMemory> mem = em.GetBuffer<SoulMemory>(mourner);
            int n = 0;
            for (int i = 0; i < mem.Length; i++)
            {
                if (mem[i].m_Subject == subject) n++;
            }
            return n;
        }
    }
}
