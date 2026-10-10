using Colossal.Serialization.Entities;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// One directed relationship edge. Every known citizen gets one; both sides
    /// keep their own edge (affinity is directed: A may adore B while B is wary).
    /// </summary>
    public struct SoulEdge : IBufferElementData, ISerializable
    {
        public Entity m_Other;
        public sbyte m_Affinity;   // -100..100
        public byte m_Flags;       // SoulEdgeFlags bits
        public ushort m_Moments;   // shared moments counter
        public uint m_LastDay;     // last interaction day (decay clock)

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Other);
            writer.Write(m_Affinity);
            writer.Write(m_Flags);
            writer.Write(m_Moments);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("edge.version", "SoulEdge read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Other);
            reader.Read(out m_Affinity);
            reader.Read(out m_Flags);
            reader.Read(out m_Moments);
            reader.Read(out m_LastDay);
        }
    }

    public static class SoulEdgeFlags
    {
        public const byte Known = 1 << 0;
        public const byte Friend = 1 << 1;
        public const byte Close = 1 << 2;
        public const byte Family = 1 << 3;
        public const byte Rival = 1 << 4;
        public const byte Colleague = 1 << 5;

        public static bool Has(byte flags, byte bit) { return (flags & bit) != 0; }

        public static bool IsFamily(byte flags) { return (flags & Family) != 0; }
    }

    /// <summary>Pure edge-list operations — no ECS types, unit-testable with stub buffers.</summary>
    public static class SoulEdgeUtil
    {
        public static int Find(DynamicBuffer<SoulEdge> edges, Entity other)
        {
            for (int i = 0; i < edges.Length; i++)
            {
                if (edges[i].m_Other == other) return i;
            }
            return -1;
        }

        /// <summary>
        /// Applies an interaction. Adds the edge if missing (respecting the cap by
        /// evicting the weakest non-family tie). Returns the index touched, or -1
        /// if even eviction could not make room (all family/close) — caller skips.
        /// </summary>
        public static int Touch(DynamicBuffer<SoulEdge> edges, Entity other, int delta, uint day, byte venueFlags, bool forceFamily)
        {
            int idx = Find(edges, other);
            if (idx < 0)
            {
                if (edges.Length >= SoulConsts.MaxEdges && !EvictWeakest(edges, other)) return -1;
                SoulEdge fresh = default(SoulEdge);
                fresh.m_Other = other;
                fresh.m_Affinity = 0;
                fresh.m_Flags = SoulEdgeFlags.Known;
                fresh.m_Moments = 0;
                fresh.m_LastDay = 0;
                edges.Add(fresh);
                idx = edges.Length - 1;
            }

            SoulEdge e = edges[idx];
            e.m_Affinity = SoulMath.ToSbyte(SoulMath.Clamp(e.m_Affinity + delta, -100, 100));
            e.m_Moments = (ushort)SoulMath.Clamp(e.m_Moments + 1, 0, ushort.MaxValue);
            e.m_LastDay = day;
            if (forceFamily) e.m_Flags |= SoulEdgeFlags.Family;
            if ((venueFlags & SoulEdgeFlags.Colleague) != 0) e.m_Flags |= SoulEdgeFlags.Colleague;
            edges[idx] = e;
            return idx;
        }

        /// <summary>
        /// Recomputes level flags from affinity. Returns a change mask so callers
        /// can journal transitions: 1=becamefriend 2=becameclose 4=fellout 8=becamerival.
        /// </summary>
        public static byte ApplyThresholds(DynamicBuffer<SoulEdge> edges, int idx)
        {
            SoulEdge e = edges[idx];
            byte changes = 0;
            int aff = e.m_Affinity;

            bool wasFriend = SoulEdgeFlags.Has(e.m_Flags, SoulEdgeFlags.Friend);
            if (!wasFriend && aff >= SoulConsts.FriendAffinity)
            {
                e.m_Flags |= SoulEdgeFlags.Friend;
                changes |= 1;
            }
            bool wasClose = SoulEdgeFlags.Has(e.m_Flags, SoulEdgeFlags.Close);
            if (!wasClose && aff >= SoulConsts.CloseAffinity)
            {
                e.m_Flags |= SoulEdgeFlags.Close | SoulEdgeFlags.Friend;
                changes |= 2;
            }
            if (aff < SoulConsts.AcquaintanceAffinity && (wasFriend || wasClose || SoulEdgeFlags.Has(e.m_Flags, SoulEdgeFlags.Known)))
            {
                if (!SoulEdgeFlags.Has(e.m_Flags, SoulEdgeFlags.Family))
                {
                    e.m_Flags &= unchecked((byte)~(SoulEdgeFlags.Friend | SoulEdgeFlags.Close | SoulEdgeFlags.Known));
                    changes |= 4;
                }
            }
            bool wasRival = SoulEdgeFlags.Has(e.m_Flags, SoulEdgeFlags.Rival);
            if (!wasRival && aff <= SoulConsts.RivalAffinity)
            {
                e.m_Flags |= SoulEdgeFlags.Rival;
                changes |= 8;
            }
            edges[idx] = e;
            return changes;
        }

        /// <summary>Evicts the lowest-|affinity| non-family, non-close edge. False if none eligible.</summary>
        public static bool EvictWeakest(DynamicBuffer<SoulEdge> edges, Entity keep)
        {
            int victim = -1;
            int weakest = int.MaxValue;
            for (int i = 0; i < edges.Length; i++)
            {
                if (edges[i].m_Other == keep) continue;
                byte f = edges[i].m_Flags;
                if (SoulEdgeFlags.IsFamily(f) || SoulEdgeFlags.Has(f, SoulEdgeFlags.Close)) continue;
                int mag = SoulMath.Abs(edges[i].m_Affinity);
                if (mag < weakest) { weakest = mag; victim = i; }
            }
            if (victim < 0) return false;
            edges.RemoveAt(victim);
            return true;
        }
    }

    /// <summary>One remembered life event (ring buffer, oldest dropped at cap).</summary>
    public struct SoulMemory : IBufferElementData, ISerializable
    {
        public uint m_Day;
        public SoulEventKind m_Kind; // byte-sized enum
        public short m_Value;        // magnitude (affinity delta, severity, ...)
        public Entity m_Subject;     // who it was about (Entity.Null for solo events)

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Day);
            writer.Write((byte)m_Kind);
            writer.Write(m_Value);
            writer.Write(m_Subject);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("memory.version", "SoulMemory read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Day);
            byte kind;
            reader.Read(out kind);
            m_Kind = (SoulEventKind)kind;
            reader.Read(out m_Value);
            reader.Read(out m_Subject);
        }
    }

    /// <summary>Long-term journal entry mirrored from memory (M4 surfaces these as text).</summary>
    public struct SoulJournalEntry : IBufferElementData, ISerializable
    {
        public uint m_Day;
        public SoulEventKind m_Kind;
        public short m_Value;
        public Entity m_Subject;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Day);
            writer.Write((byte)m_Kind);
            writer.Write(m_Value);
            writer.Write(m_Subject);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("journal.version", "SoulJournalEntry read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Day);
            byte kind;
            reader.Read(out kind);
            m_Kind = (SoulEventKind)kind;
            reader.Read(out m_Value);
            reader.Read(out m_Subject);
        }
    }

    /// <summary>Ring-buffer helpers (pure; stub-buffer friendly).</summary>
    public static class SoulMemUtil
    {
        /// <summary>Appends, dropping the oldest entry once the cap is reached.</summary>
        public static void Push(DynamicBuffer<SoulMemory> ring, uint day, SoulEventKind kind, short value, Entity subject)
        {
            if (ring.Length >= SoulConsts.MaxMemories) ring.RemoveAt(0);
            SoulMemory m = default(SoulMemory);
            m.m_Day = day;
            m.m_Kind = kind;
            m.m_Value = value;
            m.m_Subject = subject;
            ring.Add(m);
        }

        public static void PushJournal(DynamicBuffer<SoulJournalEntry> ring, uint day, SoulEventKind kind, short value, Entity subject)
        {
            if (ring.Length >= SoulConsts.MaxJournal) ring.RemoveAt(0);
            SoulJournalEntry j = default(SoulJournalEntry);
            j.m_Day = day;
            j.m_Kind = kind;
            j.m_Value = value;
            j.m_Subject = subject;
            ring.Add(j);
        }
    }
}
