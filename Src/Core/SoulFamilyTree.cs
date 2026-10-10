// Citizen Soul -- SoulFamilyTree (M5a): one-line family card for a soul (F60 groundwork).
using Unity.Entities;

namespace CitizenSoul
{
    public static class SoulFamilyTree
    {
        public static string Describe(EntityManager em, Entity e)
        {
            if (!em.HasComponent<SoulFamily>(e)) return "no family record";
            SoulFamily f = em.GetComponentData<SoulFamily>(e);
            string s = "lineage " + f.m_Lineage + ", generation " + f.m_Generation;
            if ((f.m_Flags & SoulFamilyFlags.Resolved) == 0) s += ", unsettled";
            if ((f.m_Flags & SoulFamilyFlags.Widowed) != 0) s += ", widowed";
            if ((f.m_Flags & SoulFamilyFlags.Divorced) != 0) s += ", divorced";
            if ((f.m_Flags & SoulFamilyFlags.Grandparent) != 0) s += ", grandparent";
            if ((f.m_Flags & SoulFamilyFlags.Affair) != 0) s += ", carrying a secret";
            if (f.m_ParentA != Entity.Null) s += ", child of #" + f.m_ParentA.Index;
            if (f.m_ParentB != Entity.Null) s += " and #" + f.m_ParentB.Index;
            return s;
        }
    }
}
