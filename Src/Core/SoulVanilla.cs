using System;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// The ONE place this mod reads vanilla CS2 citizen components. If a vanilla
    /// field name ever fails against the real Game.dll, the fix lands here and
    /// nowhere else. All accessors are exception-safe: a destroyed entity or a
    /// missing component degrades to false, never throws into a hot loop.
    ///
    /// Grounded fields (ps1ke reverse-engineered docs):
    ///   Game.Citizens.HouseholdMember.m_Household      — home venue
    ///   Game.Citizens.Worker.m_Workplace               — work venue
    ///   Game.Citizens.CurrentBuilding.m_CurrentBuilding — current location
    /// </summary>
    internal static class SoulVanilla
    {
        public static bool TryGetHome(EntityManager em, Entity e, out Entity home)
        {
            home = Entity.Null;
            try
            {
                if (!em.Exists(e)) return false;
                if (!em.HasComponent<Game.Citizens.HouseholdMember>(e)) return false;
                home = em.GetComponentData<Game.Citizens.HouseholdMember>(e).m_Household;
                return home != Entity.Null;
            }
            catch (Exception ex)
            {
                SoulLog.Exception("vanilla.home", ex);
                return false;
            }
        }

        public static bool TryGetWorkplace(EntityManager em, Entity e, out Entity work)
        {
            work = Entity.Null;
            try
            {
                if (!em.Exists(e)) return false;
                if (!em.HasComponent<Game.Citizens.Worker>(e)) return false;
                work = em.GetComponentData<Game.Citizens.Worker>(e).m_Workplace;
                return work != Entity.Null;
            }
            catch (Exception ex)
            {
                SoulLog.Exception("vanilla.work", ex);
                return false;
            }
        }

        public static bool TryGetBuilding(EntityManager em, Entity e, out Entity building)
        {
            building = Entity.Null;
            try
            {
                if (!em.Exists(e)) return false;
                if (!em.HasComponent<Game.Citizens.CurrentBuilding>(e)) return false;
                building = em.GetComponentData<Game.Citizens.CurrentBuilding>(e).m_CurrentBuilding;
                return building != Entity.Null;
            }
            catch (Exception ex)
            {
                SoulLog.Exception("vanilla.building", ex);
                return false;
            }
        }

        public static bool TryGetHealth(EntityManager em, Entity e, out byte health)
        {
            health = 0;
            try
            {
                if (!em.Exists(e)) return false;
                if (!em.HasComponent<Game.Citizens.Citizen>(e)) return false;
                health = em.GetComponentData<Game.Citizens.Citizen>(e).m_Health;
                return true;
            }
            catch (Exception ex)
            {
                SoulLog.Exception("vanilla.health", ex);
                return false;
            }
        }

        /// <summary>Vanilla pseudo-random byte used to salt a new soul's distribution.</summary>
        public static byte TryGetPseudoRandom(EntityManager em, Entity e)
        {
            try
            {
                if (em.Exists(e) && em.HasComponent<Game.Citizens.Citizen>(e))
                {
                    return (byte)em.GetComponentData<Game.Citizens.Citizen>(e).m_PseudoRandom;
                }
            }
            catch (Exception ex)
            {
                SoulLog.Exception("vanilla.pseudo", ex);
            }
            return 0;
        }

        /// <summary>
        /// Venue namespace key for co-location sorting: kind in the high nibble so
        /// homes, workplaces and buildings never collide. Kind: 0=home 1=work 2=building.
        /// </summary>
        public static long VenueKey(Entity venue, byte kind)
        {
            unchecked
            {
                long k = (long)kind << 60;
                k |= (long)(uint)venue.Index << 16;
                k |= (uint)venue.Version;
                return k;
            }
        }
    }
}
