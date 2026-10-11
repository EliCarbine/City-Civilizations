// Citizen Soul -- EP2 Path (F191-F198). The road each soul walks: school
// years quietly building them up, the crossroads at the end of them, a dream
// chosen (or choosing them), detours, reroutes, late bloomers who rise
// anyway, and the few who make it to the very top of the field. Luck is a
// real factor, exactly like life.

using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    public partial class PathSystem : SoulSystemBase
    {
        private const int MigrateBudget = 64;
        private const int ChoosePerMille = 30;   // daily pull toward a calling
        private const uint SelfStartAge = 3500;  // dreams pull early sometimes

        private EntityQuery m_All;
        private EntityQuery m_Desk;

        protected override int Interval { get { return 4096; } }
        protected override int Lanes { get { return 64; } }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_All = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>());
            m_Desk = GetEntityQuery(ComponentType.ReadOnly<SoulNews>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            EntityManager em = EntityManager;

            NativeArray<Entity> souls = m_All.ToEntityArray(Allocator.Temp);
            try
            {
                int budget = 0;
                for (int i = 0; i < souls.Length; i++)
                {
                    Entity e = souls[i];
                    if (!Matches(e, lane, Lanes)) continue;
                    if (em.HasComponent<SoulPath>(e)) continue;
                    if (budget >= MigrateBudget) break;
                    budget++;
                    try { em.AddComponentData(e, SoulPath.Fresh(day)); }
                    catch (Exception ex)
                    {
                        DisableSelf("path migrate: " + ex.Message);
                        return;
                    }
                }

                for (int i = 0; i < souls.Length; i++)
                {
                    Entity e = souls[i];
                    if (!Matches(e, lane, Lanes)) continue;
                    if (!em.HasComponent<SoulPath>(e)) continue;
                    try { Advance(em, e, day); }
                    catch (Exception) { /* one sick soul never stops the city */ }
                }
            }
            finally
            {
                souls.Dispose();
            }
        }

        private void Advance(EntityManager em, Entity e, uint day)
        {
            SoulPath p = em.GetComponentData<SoulPath>(e);
            if (p.m_LastDay == day) return;

            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            uint age = day >= meta.m_BirthDay ? day - meta.m_BirthDay : 0u;
            int luck = 0;
            if (em.HasComponent<SoulWallet>(e)) luck = em.GetComponentData<SoulWallet>(e).m_Luck;
            uint salt = SoulEp2Util.Salt(e);
            bool grown = age >= (uint)SoulEp2Util.TeenDay;

            // F191 -- school years: every soul builds quietly toward what
            // they might become.
            if (!grown && p.m_Progress < SoulEp2Util.ProgressCap)
                p.m_Progress = (ushort)Math.Min(SoulEp2Util.ProgressCap, p.m_Progress + 2);

            // F192 -- the crossroads at the end of school.
            if (grown && (p.m_Flags & SoulPathFlags.Crossroads) == 0)
            {
                p.m_Flags |= SoulPathFlags.Crossroads;
                SoulFeel.Note(em, e, day, SoulEventKind.CrossroadsOn, 0, e);
                SoulFeel.Pulse(em, e, 3);
            }

            // F193 -- a dream, chosen or choosing.
            if (grown && (p.m_Flags & SoulPathFlags.Chosen) == 0)
            {
                bool selfStart = luck >= SoulEp2Util.SelfMadeLuck;
                if (selfStart || SoulEp2Util.Roll(salt, day, ChoosePerMille))
                {
                    if (selfStart) p.m_Flags |= SoulPathFlags.SelfMade;
                    p.m_Flags |= SoulPathFlags.Chosen;
                    p.m_Dream = (byte)SoulEp2Util.Range(salt, day + 1u, 12, 95u);
                    p.m_ChosenDay = day;
                    SoulFeel.Note(em, e, day, SoulEventKind.ChoseTheRoadOn, (short)p.m_Dream, e);
                    SoulFeel.Pulse(em, e, 6);
                    Scoop(em, SoulWire.Life, 2, p.m_Dream, e, day);
                }
            }

            // F194 -- the road bends: a reroute taken.
            if ((p.m_Flags & SoulPathFlags.Chosen) != 0 && (p.m_Flags & SoulPathFlags.Rerouted) == 0 &&
                SoulEp2Util.Roll(salt, day + 2u, SoulEp2Util.ReroutePerMille))
            {
                p.m_Flags |= SoulPathFlags.Rerouted;
                p.m_Dream = (byte)SoulEp2Util.Range(salt, day + 2u, 12, 96u);
                if (Gate(em, e, day))
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.ReroutedOn, (short)p.m_Dream, e);
                    Scoop(em, SoulWire.Life, 2, p.m_Dream, e, day);
                }
            }

            // F195 -- deferred: the dream goes on a shelf for a while.
            if ((p.m_Flags & SoulPathFlags.Chosen) != 0 && (p.m_Flags & SoulPathFlags.Deferred) == 0 &&
                SoulEp2Util.Roll(salt, day + 3u, SoulEp2Util.DeferPerMille))
            {
                p.m_Flags |= SoulPathFlags.Deferred;
                if (Gate(em, e, day))
                    SoulFeel.Note(em, e, day, SoulEventKind.DeferredOn, 0, e);
            }

            // F196 -- progress on the road. Study compounds, luck nudges.
            if ((p.m_Flags & SoulPathFlags.Chosen) != 0 && p.m_Progress < SoulEp2Util.ProgressCap)
            {
                int gain = SoulEp2Util.ProgressWork + (luck > 0 ? luck / 4 : 0);
                p.m_Progress = (ushort)Math.Min(SoulEp2Util.ProgressCap, p.m_Progress + Math.Max(1, gain));
            }

            // F197 -- rising, and the rare late bloom alongside it.
            if ((p.m_Flags & SoulPathFlags.Rising) == 0 && p.m_Progress >= SoulEp2Util.RiseAt)
            {
                p.m_Flags |= SoulPathFlags.Rising;
                p.m_RiseDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.RisingOn, (short)p.m_Progress, e);
                SoulFeel.Pulse(em, e, 6);
                Scoop(em, SoulWire.Life, 3, p.m_Progress, e, day);

                if (p.m_ChosenDay >= meta.m_BirthDay + (uint)SoulEp2Util.TeenDay + 21600u &&
                    SoulEp2Util.Roll(salt, day + 4u, SoulEp2Util.LateBloomPerMille))
                {
                    p.m_Flags |= SoulPathFlags.LateBloom;
                    SoulFeel.Note(em, e, day, SoulEventKind.LateBloomOn, (short)p.m_Progress, e);
                    Scoop(em, SoulWire.Life, 3, p.m_Progress, e, day);
                }
            }

            // F198 -- the top of the field: talent, work, a little luck.
            if ((p.m_Flags & SoulPathFlags.Topped) == 0 && (p.m_Flags & SoulPathFlags.Rising) != 0 &&
                p.m_Progress >= SoulEp2Util.TopAt &&
                luck + (int)SoulEp2Util.Range(salt, day + 5u, 13, 97u) - 6 >= SoulEp2Util.TopLuckFloor)
            {
                p.m_Flags |= SoulPathFlags.Topped;
                SoulFeel.Note(em, e, day, SoulEventKind.MadeTheTopOn, (short)p.m_Progress, e);
                SoulFeel.Pulse(em, e, 12);
                Scoop(em, SoulWire.Life, 4, p.m_Progress, e, day);
            }

            p.m_LastDay = day;
            em.SetComponentData(e, p);
        }

        private bool Gate(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulWallet>(e)) return true;
            SoulWallet w = em.GetComponentData<SoulWallet>(e);
            if (w.m_LastNoteDay != 0u && day - w.m_LastNoteDay < (uint)SoulEp2Util.NoteGapDays) return false;
            w.m_LastNoteDay = day;
            em.SetComponentData(e, w);
            return true;
        }

        private void Scoop(EntityManager em, byte kind, byte prio, int value, Entity about, uint day)
        {
            if (m_Desk.CalculateEntityCount() != 1) return;
            Entity desk = m_Desk.GetSingletonEntity();
            if (!em.HasComponent<SoulNews>(desk)) return;
            SoulEp2Util.Scoop(em, desk, kind, prio, value, about, day);
        }
    }
}
