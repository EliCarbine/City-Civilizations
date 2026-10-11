// Citizen Soul -- EP2 Broadcast (F179-F182). The screen: a lineup of shows
// that turns over in seasons, buzz that cools like everything else, and the
// quiet ways television gets into a life -- the premiere you caught, the show
// you found, the cartoons you grew up on. The city's TV is citywide weather;
// what airs becomes word of mouth when the newsroom picks it up.
//
// The TV set (SoulTv + SoulAir lineup) is created and owned here. The desk
// (SoulNews) belongs to the NewsroomSystem; scoops reach it through the
// shared utility wrapper.

using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    public partial class BroadcastSystem : SoulSystemBase
    {
        private const int LineupSize = 4;        // slots on the dial
        private const int SeasonDays = 26;       // a season runs this long
        private const int WatchPerMille = 260;   // daily chance a soul watches
        private const int DevotePerMille = 40;   // watchers who find THE show
        private const int CartoonPerMille = 55;  // pull toward Saturday cartoons
        private const int CoolPerMille = 30;     // hotness cools by this much/day
        private const int WatchBudget = 192;     // souls walked per cadence

        private EntityQuery m_Tv;
        private EntityQuery m_Souls;
        private EntityQuery m_Desk;

        protected override int Interval { get { return 4096; } }
        protected override int Lanes { get { return 64; } }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Tv = GetEntityQuery(ComponentType.ReadOnly<SoulTv>());
            m_Souls = GetEntityQuery(
                ComponentType.ReadOnly<SoulMeta>());
            m_Desk = GetEntityQuery(ComponentType.ReadOnly<SoulNews>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            if (lane != 0) return; // one dial, one city: a single lane runs the screen

            EntityManager em = EntityManager;

            try { BroadcastStep(em, day); }
            catch (Exception ex) { DisableSelf("broadcast: " + ex.Message); }
        }

        private void BroadcastStep(EntityManager em, uint day)
        {
            if (m_Tv.CalculateEntityCount() == 0)
            {
                Entity reg = em.CreateEntity();
                em.AddComponentData(reg, SoulTv.Fresh(day));
                em.AddBuffer<SoulAir>(reg);
                DynamicBuffer<SoulAir> fresh = em.GetBuffer<SoulAir>(reg);
                RollSeason(reg, fresh, day, 1);
                SoulLog.Once("tv.registry", "BroadcastSystem: the city's screen switched on.");
                return;
            }
            if (m_Tv.CalculateEntityCount() != 1) return;

            Entity tv = m_Tv.GetSingletonEntity();
            SoulTv t = em.GetComponentData<SoulTv>(tv);
            if (t.m_LastDay == day) return;
            t.m_LastDay = day;

            DynamicBuffer<SoulAir> lineup = em.GetBuffer<SoulAir>(tv);

            // Season roll: a fresh lineup premieres and the dial starts over.
            if (day - t.m_SeasonStart >= SeasonDays)
            {
                t.m_Season += 1;
                RollSeason(tv, lineup, day, t.m_Season);
                Scoop(em, 2, 40, tv, day);
            }

            // Buzz cools; nothing stays hot forever.
            for (int i = 0; i < lineup.Length; i++)
            {
                SoulAir air = lineup[i];
                if (air.m_Kind == 0) continue;
                short cool = (short)(air.m_Hotness * CoolPerMille / 1000);
                if (cool < 1) cool = 1;
                air.m_Hotness = (short)(air.m_Hotness - cool);
                if (air.m_Hotness < 0) air.m_Hotness = 0;
                lineup[i] = air;
            }

            em.SetComponentData(tv, t);

            // The city watches. A single lane walks the souls.
            NativeArray<Entity> souls = m_Souls.ToEntityArray(Allocator.Temp);
            try
            {
                int watched = 0;
                for (int i = 0; i < souls.Length && watched < WatchBudget; i++)
                {
                    Entity e = souls[i];
                    uint h = Hash(e, day);
                    if (h % 1000 >= WatchPerMille) continue;
                    watched++;

                    if (lineup.Length == 0) continue;
                    SoulAir air = lineup[(int)((h / 1000) % (uint)lineup.Length)];
                    if (air.m_Kind == 0) continue;

                    bool fresh = !em.HasComponent<SoulViewer>(e);
                    SoulViewer v;
                    if (fresh)
                    {
                        v = default(SoulViewer);
                        v.m_Favorite = 255;
                        v.m_LastDay = day;
                    }
                    else
                    {
                        v = em.GetComponentData<SoulViewer>(e);
                        if (v.m_LastDay == day) continue;
                        v.m_LastDay = day;
                    }

                    v.m_Flags |= SoulViewerFlags.Watcher;
                    v.m_LastWatchDay = day;

                    // Caught the premiere itself.
                    if (air.m_PremiereDay == day)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.PremiereOn, (short)air.m_ShowId, tv);
                        SoulFeel.Pulse(em, e, 2);
                    }

                    // Found THE show.
                    if (v.m_Favorite == 255 && (v.m_Flags & SoulViewerFlags.Devoted) == 0
                        && ((h / 7919) % 1000) < DevotePerMille)
                    {
                        v.m_Favorite = air.m_ShowId;
                        v.m_Flags |= SoulViewerFlags.Devoted;
                        SoulFeel.Note(em, e, day, SoulEventKind.FoundShowOn, (short)air.m_ShowId, tv);
                        SoulFeel.Pulse(em, e, 3);
                    }

                    // Grew up on Saturday cartoons.
                    if ((v.m_Flags & SoulViewerFlags.CartoonKid) == 0
                        && ((h / 104729) % 1000) < CartoonPerMille)
                    {
                        v.m_Flags |= SoulViewerFlags.CartoonKid;
                        SoulFeel.Note(em, e, day, SoulEventKind.CartoonYearsOn, (short)LineupSize, tv);
                    }

                    if (fresh) em.AddComponentData(e, v);
                    else em.SetComponentData(e, v);
                }
            }
            finally { souls.Dispose(); }
        }

        private void RollSeason(Entity tv, DynamicBuffer<SoulAir> lineup, uint day, uint season)
        {
            lineup.Clear();
            uint seed = Hash(tv, season * 7919u);
            for (int i = 0; i < LineupSize; i++)
            {
                uint hh = Hash(tv, seed + (uint)(i + 1) * 2654435761u);
                SoulAir air = default(SoulAir);
                air.m_Kind = (byte)(1 + (i % 3)); // SoulShowKind slot: nothing airs empty
                air.m_Name1 = (byte)(hh % 24);
                air.m_Name2 = (byte)((hh / 24) % 24);
                air.m_ShowId = (byte)((hh / 24 / 24) % 24);
                air.m_Hotness = 30;
                air.m_PremiereDay = day;
                lineup.Add(air);
            }
            SoulLog.Once("tv.season." + season, "BroadcastSystem: season " + season + " premiered.");
        }

        private void Scoop(EntityManager em, byte prio, int value, Entity about, uint day)
        {
            if (m_Desk.CalculateEntityCount() != 1) return;
            Entity desk = m_Desk.GetSingletonEntity();
            SoulEp2Util.Scoop(em, desk, SoulWire.Screen, prio, value, about, day);
        }

        private static uint Hash(Entity e, uint k)
        {
            uint h = (uint)e.Index * 2654435761u;
            h ^= k + 0x9E3779B9u + (h << 6) + (h >> 2);
            return h;
        }
    }
}
