// Citizen Soul -- EP1 shared helpers (roll, salt, companions, dual touch).
// EP1 systems mirror the house pattern established by RitualsSystem (M5m):
// a per-system Roll hash, salt from entity index, friend picks off the edge
// buffer, and mutual affinity bumps guarded by HasBuffer checks.
using System;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// EP1 statics. Roll mirrors the house hash exactly:
    /// h = day * 747796405 + salt + 2246822519; h ^= h >> 15;
    /// h *= 2654435761; h ^= h >> 13; return h % mod.
    /// </summary>
    internal static class SoulEp1Util
    {
        public static uint Salt(Entity e) { return (uint)e.Index * 2654435761u; }

        public static uint Roll(uint day, uint salt, uint mod)
        {
            if (mod == 0u) return 0u;
            uint h = day * 747796405u + salt + 2246822519u;
            h ^= h >> 15;
            h *= 2654435761u;
            h ^= h >> 13;
            return h % mod;
        }

        /// <summary>Best friend (or first soul above minAffinity) from the edge buffer.</summary>
        public static Entity Companion(EntityManager em, Entity e, int minAffinity)
        {
            if (!em.HasBuffer<SoulEdge>(e)) return Entity.Null;
            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
            Entity best = Entity.Null;
            int bestA = minAffinity;
            for (int i = 0; i < edges.Length; i++)
            {
                SoulEdge edge = edges[i];
                if (edge.m_Affinity < bestA) continue;
                Entity other = edge.m_Other;
                if (other == e || !em.Exists(other)) continue;
                if (!em.HasComponent<SoulMeta>(other)) continue;
                best = other;
                bestA = edge.m_Affinity;
            }
            return best;
        }

        /// <summary>Touch the same pair in both directions, never fatal.</summary>
        public static void BumpBoth(EntityManager em, Entity a, Entity b, int delta, uint day)
        {
            if (a == b || b == Entity.Null) return;
            try
            {
                if (em.HasBuffer<SoulEdge>(a)) SoulEdgeUtil.Touch(em.GetBuffer<SoulEdge>(a), b, delta, day, 0, false);
                if (em.HasBuffer<SoulEdge>(b)) SoulEdgeUtil.Touch(em.GetBuffer<SoulEdge>(b), a, delta, day, 0, false);
            }
            catch (Exception ex)
            {
                SoulLog.Once("ep1.bump", "ep1 link bump failed: " + ex.Message);
            }
        }
    }
}
