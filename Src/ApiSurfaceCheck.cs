// ---------------------------------------------------------------------------
// ApiSurfaceCheck - compile-time only. Every call below pins an exact house
// signature that the M2 systems rely on; if a line stops compiling, an API
// drifted (or the stub grew stale). Nothing here runs at play time.
//
// (Formerly the temporary compiler-oracle probe - kept as a regression net.)
// ---------------------------------------------------------------------------
using Unity.Entities;

namespace CitizenSoul
{
    internal sealed class ApiSurfaceCheck : SoulSystemBase
    {
        protected override int Lanes => 8;

        protected override int Interval => 1024;

        protected override void OnSoulUpdate()
        {
        }

        private void Calls(EntityManager em, Entity e)
        {
            if (AdvanceDay(out uint day, out int lane))
            {
                _ = Matches(e, lane, Lanes);
            }
            SoulLog.Once("api.check", "api surface check");
            SoulLog.Throttled("api.check", "api surface check", 1000);
            SoulFeel.Pulse(em, e, 1);
            SoulFeel.Note(em, e, 0u, SoulEventKind.Born, 0, e);

            DynamicBuffer<SoulEdge> buf = em.GetBuffer<SoulEdge>(e);
            _ = SoulEdgeUtil.Find(buf, e);
            SoulEdgeUtil.Touch(buf, e, 0, 0u, 0, false);
            SoulEdge probeEdge = buf[0];
            probeEdge.m_Flags |= SoulEdgeFlags.Known;
            buf[0] = probeEdge;
            _ = SoulEdgeFlags.Has(SoulEdgeFlags.Feud, SoulEdgeFlags.Friend);

            SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
            _ = SoulQuirks.Has(p.m_Quirk, SoulQuirks.Hothead);
            Unity.Mathematics.Random r = SoulRng.For(p.m_SoulSeed, e, 0u);
            _ = r.NextInt(0, 1000);

            SoulRomance rom = em.GetComponentData<SoulRomance>(e);
            _ = rom.m_Stage;
            SoulFirsts f = em.GetComponentData<SoulFirsts>(e);
            f.m_Bits |= SoulFirstsFlags.CloseFriend;
            _ = f.m_LastDay;
            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            _ = meta.m_BirthDay;
            _ = SoulVanilla.TryGetWorkplace(em, e, out Entity work);
            _ = e.Index;
        }
    }
}
