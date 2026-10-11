// Citizen Soul -- EP2 Newsroom (F183). The evening news: the desk watches
// the wire for the day's best scoops, promotes one to the lead, keeps a
// short memory of headlines, and sends the broadcast out into the city.
// Only a handful of souls catch an airing -- the news landing on you is an
// event -- and the soul a story is about carries it hardest.
//
// The desk (SoulNews + SoulScoop wire + SoulHeadline ring) is created and
// owned here. Every other system reaches it through SoulEp2Util.Scoop.

using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    public partial class NewsroomSystem : SoulSystemBase
    {
        private const int AirtimePerMille = 30;  // souls who catch the evening news
        private const int AirReactionCap = 6;    // reactions logged per airing
        private const int HeadlineCap = 16;      // headline ring holds this many
        private const int LeadMinPrio = 1;       // below this, a scoop never leads

        private EntityQuery m_Desk;
        private EntityQuery m_Souls;

        protected override int Interval { get { return 4096; } }
        protected override int Lanes { get { return 64; } }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Desk = GetEntityQuery(ComponentType.ReadOnly<SoulNews>());
            m_Souls = GetEntityQuery(ComponentType.ReadOnly<SoulMeta>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            if (lane != 0) return; // one desk, one broadcast

            EntityManager em = EntityManager;

            try { NewsStep(em, day); }
            catch (Exception ex) { DisableSelf("newsroom: " + ex.Message); }
        }

        private void NewsStep(EntityManager em, uint day)
        {
            if (m_Desk.CalculateEntityCount() == 0)
            {
                Entity d = em.CreateEntity();
                em.AddComponentData(d, SoulNews.Fresh(day));
                em.AddBuffer<SoulScoop>(d);
                em.AddBuffer<SoulHeadline>(d);
                SoulLog.Once("desk.registry", "NewsroomSystem: the news desk opened.");
                return;
            }
            if (m_Desk.CalculateEntityCount() != 1) return;

            Entity desk = m_Desk.GetSingletonEntity();
            SoulNews n = em.GetComponentData<SoulNews>(desk);
            if (n.m_LastDay == day) return;
            n.m_LastDay = day;

            DynamicBuffer<SoulScoop> wire = em.GetBuffer<SoulScoop>(desk);
            if (wire.Length == 0)
            {
                em.SetComponentData(desk, n);
                return;
            }

            // Promote the day's lead: heaviest scoop; ties go to the freshest.
            int best = -1;
            for (int i = 0; i < wire.Length; i++)
            {
                SoulScoop s = wire[i];
                if (s.m_Prio < LeadMinPrio) continue;
                if (best < 0
                    || s.m_Prio > wire[best].m_Prio
                    || (s.m_Prio == wire[best].m_Prio && s.m_Day >= wire[best].m_Day))
                {
                    best = i;
                }
            }

            if (best >= 0)
            {
                SoulScoop lead = wire[best];
                n.m_TopKind = lead.m_Kind;
                n.m_TopPrio = lead.m_Prio;
                n.m_TopValue = lead.m_Value;
                n.m_TopAbout = lead.m_About;
                n.m_TopDay = day;

                DynamicBuffer<SoulHeadline> ring = em.GetBuffer<SoulHeadline>(desk);
                SoulHeadline hd = default(SoulHeadline);
                hd.m_Kind = lead.m_Kind;
                hd.m_Value = lead.m_Value;
                hd.m_About = lead.m_About;
                hd.m_Day = lead.m_Day;
                if (ring.Length >= HeadlineCap) ring.RemoveAt(0);
                ring.Add(hd);
            }
            em.SetComponentData(desk, n);

            // The story lands on the souls it is about -- once, always.
            if (best >= 0)
            {
                SoulScoop lead0 = wire[best];
                if (lead0.m_About != Entity.Null && em.Exists(lead0.m_About))
                {
                    SoulFeel.Note(em, lead0.m_About, day, SoulEventKind.MadeTheNewsOn, lead0.m_Value, lead0.m_About);
                    SoulFeel.Pulse(em, lead0.m_About, 3);
                }
            }

            // The broadcast goes out. A single lane walks the souls; the news
            // lands on a handful, never the whole city at once.
            NativeArray<Entity> souls = m_Souls.ToEntityArray(Allocator.Temp);
            try
            {
                int reached = 0;
                for (int i = 0; i < souls.Length && reached < AirReactionCap; i++)
                {
                    Entity e = souls[i];
                    uint h = Hash(e, day * 31u + 7u);
                    if (h % 1000 >= AirtimePerMille) continue;
                    reached++;

                    if (best >= 0)
                    {
                        SoulScoop lead = wire[best];
                        SoulFeel.Note(em, e, day, SoulEventKind.SawTheNewsOn, lead.m_Value, lead.m_About);
                        SoulFeel.Pulse(em, e, 1);
                    }

                    // The evening news becomes a ritual for a few.
                    if (((h / 4099) % 8) == 0)
                    {
                        bool fresh = !em.HasComponent<SoulViewer>(e);
                        SoulViewer v = fresh ? default(SoulViewer) : em.GetComponentData<SoulViewer>(e);
                        if (fresh)
                        {
                            v.m_Favorite = 255;
                            v.m_LastDay = day;
                        }
                        if ((v.m_Flags & SoulViewerFlags.NewsHound) == 0)
                        {
                            v.m_Flags |= SoulViewerFlags.NewsHound;
                            if (fresh) em.AddComponentData(e, v);
                            else em.SetComponentData(e, v);
                        }
                    }
                }
            }
            finally { souls.Dispose(); }

            wire.Clear(); // consumed
        }

        private static uint Hash(Entity e, uint k)
        {
            uint h = (uint)e.Index * 2654435761u;
            h ^= k + 0x9E3779B9u + (h << 6) + (h >> 2);
            return h;
        }
    }
}
