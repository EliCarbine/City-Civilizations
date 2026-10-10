using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// M3 (F47): companion animals — lonely souls adopt, the bond grows daily, comfort
    /// pulses steady the mood, and when the anchor finally slips the grief is real.
    /// </summary>
    public partial class PetSystem : SoulSystemBase
    {
        protected override int Interval { get { return 4096; } }
        protected override int Lanes { get { return SoulConsts.PetLanes; } }

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
                        if (!em.HasComponent<SoulPet>(e)) em.AddComponentData(e, default(SoulPet));
                        Tick(em, e, day);
                    }
                    catch (Exception ex)
                    {
                        faults++;
                        SoulLog.Exception("pet.tick", ex);
                    }
                }
            }
            finally
            {
                souls.Dispose();
            }
            if (faults > 0)
            {
                SoulLog.Throttled("pet.faults", faults + " pet fault(s) this slice", 60000);
            }
        }

        private static void Tick(EntityManager em, Entity e, uint day)
        {
            SoulPet pet = em.GetComponentData<SoulPet>(e);
            if (pet.m_PetDay == day) return;
            pet.m_PetDay = day;
            uint salt = (uint)e.Index * 2654435761u;

            if (pet.m_Kind == 0)
            {
                // F47: lonely souls (few real edges) find a warm anchor.
                int edges = em.HasBuffer<SoulEdge>(e) ? em.GetBuffer<SoulEdge>(e).Length : 0;
                if (edges <= SoulTune.PetLonelyEdges && Roll(day, salt + 111u, 1000u) < SoulTune.PetAdoptPerMille)
                {
                    pet.m_Kind = (byte)(1 + Roll(day, salt + 112u, 3u));
                    pet.m_AdoptDay = day;
                    pet.m_Bond = 0;
                    pet.m_LastComfortDay = day;
                    SoulFeel.Note(em, e, day, SoulEventKind.PetAdopted, (short)pet.m_Kind, Entity.Null);
                    SoulFeel.Pulse(em, e, SoulTune.PetMood);
                }
            }
            else
            {
                uint lifespan = pet.m_Kind == SoulPet.KindDog ? (uint)SoulTune.DogLifespanDays
                              : pet.m_Kind == SoulPet.KindCat ? (uint)SoulTune.CatLifespanDays
                              : (uint)SoulTune.BirdLifespanDays;

                if (day - pet.m_AdoptDay >= lifespan)
                {
                    // F11/F47: the anchor slips — grief is real.
                    int bond = pet.m_Bond;
                    pet.m_Kind = 0;
                    pet.m_Bond = 0;
                    SoulFeel.Note(em, e, day, SoulEventKind.PetDied, (short)bond, Entity.Null);
                    SoulFeel.Pulse(em, e, SoulTune.PetDeathMood);
                    if (bond >= SoulTune.PetDeathTraumaBond && em.HasComponent<SoulMind>(e))
                    {
                        SoulMind mind = em.GetComponentData<SoulMind>(e);
                        mind.m_Trauma = (byte)Math.Min(255, mind.m_Trauma + SoulTune.PetDeathTrauma);
                        mind.m_ScarDay = day;
                        em.SetComponentData(e, mind);
                    }
                }
                else
                {
                    if (pet.m_Bond < SoulPet.BondMax) pet.m_Bond++;
                    if (day - pet.m_LastComfortDay >= (uint)SoulTune.PetComfortEveryDays)
                    {
                        pet.m_LastComfortDay = day;
                        SoulFeel.Pulse(em, e, SoulTune.PetComfort);   // the daily anchor
                    }
                }
            }

            em.SetComponentData(e, pet);
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
