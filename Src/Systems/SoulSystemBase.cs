using System;
using Game;
using Game.Simulation;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// Fault-isolated base for every soul system.
    ///
    /// Contract:
    ///   - OnUpdate is sealed; derived systems implement OnSoulUpdate.
    ///   - Any exception is caught, logged once per key, and after 3 faults the
    ///     system disables itself instead of spamming or stalling the city.
    ///   - AdvanceDay() owns the shared day/lane clock so every system processes
    ///     citizens in staggered slices instead of all at once (5% overhead goal).
    /// </summary>
    public abstract class SoulSystemBase : GameSystemBase
    {
        private int _faults;
        private bool _disabled;
        private uint _lastDay;
        private int _callsToday;

        /// <summary>Update interval in frames; override in derived systems.</summary>
        protected virtual int Interval { get { return 2048; } }

        /// <summary>Number of lane buckets; each citizen lands in exactly one.</summary>
        protected virtual int Lanes { get { return 32; } }

        protected bool IsDisabled { get { return _disabled; } }

        protected override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return Interval;
        }

        protected sealed override void OnUpdate()
        {
            if (_disabled) return;
            try
            {
                OnSoulUpdate();
            }
            catch (Exception ex)
            {
                _faults++;
                SoulLog.Exception("sys." + GetType().Name, ex);
                if (_faults >= 3)
                {
                    DisableSelf("3 faults");
                }
            }
        }

        protected abstract void OnSoulUpdate();

        /// <summary>
        /// Day + lane for THIS update call. Returns false only if the
        /// simulation clock is unreachable (never on a live game world).
        /// </summary>
        protected bool AdvanceDay(out uint day, out int lane)
        {
            day = 0;
            lane = 0;
            SimulationSystem sim = World.GetOrCreateSystemManaged<SimulationSystem>();
            if (sim == null)
            {
                SoulLog.Once("sys.nosim", "SimulationSystem unavailable; soul systems idle.");
                return false;
            }
            day = SoulClock.DayOf(sim.frameIndex);
            if (day != _lastDay)
            {
                _lastDay = day;
                _callsToday = 0;
            }
            lane = _callsToday % Lanes;
            _callsToday++;
            return true;
        }

        /// <summary>Lane membership test (staggered processing).</summary>
        protected static bool Matches(Entity e, int lane, int lanes)
        {
            return SoulClock.InLane(e, lane, lanes);
        }

        /// <summary>Called from OnCreate catch blocks and repeated OnUpdate faults.</summary>
        protected void DisableSelf(string why)
        {
            if (_disabled) return;
            _disabled = true;
            SoulLog.Err("[CitizenSoul] " + GetType().Name + " disabled (" + why + "); the rest of the simulation is unaffected.");
        }
    }
}
