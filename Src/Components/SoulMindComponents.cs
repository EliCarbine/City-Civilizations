using Colossal.Serialization.Entities;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// M3 (F11–F15, F17, F31–F35, F47): mental weather, chronic conditions, crime record,
    /// prison clock and companion animals. All NEW components — existing components keep
    /// their serialized field counts (ComponentVersion = 2 contract). Older saves migrate
    /// lazily: each owning system scaffolds its component on first touch.
    /// </summary>
    public struct SoulMind : IComponentData, ISerializable
    {
        public const byte FlagWithdrawn = 1;   // F13: depression withdrawal — home all day
        public const byte FlagCrowdShy = 2;    // F13: anxiety variant — avoids crowds
        public const byte FlagInTherapy = 4;   // F12: active counseling arc

        public byte m_Trauma;          // F11: 0..255, scar tissue of the mind
        public byte m_Depression;      // F13: 0..255, sustained grey
        public byte m_Worry;           // F13: 0..255, anxiety load
        public byte m_Addiction;       // F15: 0..255, the valve that owns you
        public byte m_Habit;           // F14: 0 none, 1 drink, 2 late-night eating, 3 doomscrolling
        public byte m_Flags;           // FlagWithdrawn | FlagCrowdShy | FlagInTherapy
        public byte m_Sessions;        // F12: therapy sessions attended
        public ushort m_SoberDays;     // F15: clean-day credit
        public uint m_ScarDay;         // F11: day of the most recent scar
        public uint m_LastAnnivDay;    // F11: anniversary flare cursor
        public uint m_LastHabitDay;    // F14: relief-hit cursor
        public uint m_LastSessionDay;  // F12: session cadence cursor
        public uint m_MindDay;         // daily cursor — MindSystem
        public uint m_TherapyDay;      // daily cursor — TherapySystem
        public uint m_LastInjuryDay;   // F11: violence-scar cursor

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Trauma);
            writer.Write(m_Depression);
            writer.Write(m_Worry);
            writer.Write(m_Addiction);
            writer.Write(m_Habit);
            writer.Write(m_Flags);
            writer.Write(m_Sessions);
            writer.Write(m_SoberDays);
            writer.Write(m_ScarDay);
            writer.Write(m_LastAnnivDay);
            writer.Write(m_LastHabitDay);
            writer.Write(m_LastSessionDay);
            writer.Write(m_MindDay);
            writer.Write(m_TherapyDay);
            writer.Write(m_LastInjuryDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("soulmind.future", "SoulMind read at version " + version + "; fields beyond the current contract were skipped.");
            }
            reader.Read(out m_Trauma);
            reader.Read(out m_Depression);
            reader.Read(out m_Worry);
            reader.Read(out m_Addiction);
            reader.Read(out m_Habit);
            reader.Read(out m_Flags);
            reader.Read(out m_Sessions);
            reader.Read(out m_SoberDays);
            reader.Read(out m_ScarDay);
            reader.Read(out m_LastAnnivDay);
            reader.Read(out m_LastHabitDay);
            reader.Read(out m_LastSessionDay);
            reader.Read(out m_MindDay);
            reader.Read(out m_TherapyDay);
            reader.Read(out m_LastInjuryDay);
        }
    }

    public struct SoulChronic : IComponentData, ISerializable
    {
        public const byte KindNone = 0;        // F17: healthy
        public const byte KindAsthma = 1;
        public const byte KindArthritis = 2;
        public const byte KindHypertension = 3;
        public const byte KindDiabetes = 4;
        public const byte KindVisionLoss = 5;

        public byte m_Kind;            // 0 none, else Kind*
        public byte m_Severity;        // 1..3
        public uint m_DxDay;           // diagnosis day
        public uint m_ChronicDay;      // daily cursor — ChronicSystem
        public uint m_LastRoutineDay;  // clinic/routine cadence cursor
        public uint m_LastNoteDay;     // journal cadence cursor

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Kind);
            writer.Write(m_Severity);
            writer.Write(m_DxDay);
            writer.Write(m_ChronicDay);
            writer.Write(m_LastRoutineDay);
            writer.Write(m_LastNoteDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("soulchronic.future", "SoulChronic read at version " + version + "; fields beyond the current contract were skipped.");
            }
            reader.Read(out m_Kind);
            reader.Read(out m_Severity);
            reader.Read(out m_DxDay);
            reader.Read(out m_ChronicDay);
            reader.Read(out m_LastRoutineDay);
            reader.Read(out m_LastNoteDay);
        }
    }

    public struct SoulCrime : IComponentData, ISerializable
    {
        public byte m_Notoriety;       // F31/F32/F35: 0..255 heat; cools over clean days
        public byte m_Records;         // F35: convictions on file
        public byte m_Gang;            // F33: 0 none, else gang tier 1..3
        public uint m_StreetDay;       // daily cursor — StreetSystem
        public uint m_JusticeDay;      // daily cursor — JusticeSystem
        public uint m_LastCrimeDay;    // last street crime
        public uint m_LastDecayDay;    // notoriety decay cursor

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Notoriety);
            writer.Write(m_Records);
            writer.Write(m_Gang);
            writer.Write(m_StreetDay);
            writer.Write(m_JusticeDay);
            writer.Write(m_LastCrimeDay);
            writer.Write(m_LastDecayDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("soulcrime.future", "SoulCrime read at version " + version + "; fields beyond the current contract were skipped.");
            }
            reader.Read(out m_Notoriety);
            reader.Read(out m_Records);
            reader.Read(out m_Gang);
            reader.Read(out m_StreetDay);
            reader.Read(out m_JusticeDay);
            reader.Read(out m_LastCrimeDay);
            reader.Read(out m_LastDecayDay);
        }
    }

    /// <summary>
    /// F35: serving time. Off-map is modelled as "homebound" — the social systems already
    /// skip imprisoned souls via SoulMindGate.Homebound. This component is enableable so
    /// release simply disables it and the soul keeps its history.
    /// </summary>
    public struct Imprisoned : IComponentData, IEnableableComponent, ISerializable
    {
        public uint m_SinceDay;
        public uint m_UntilDay;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_SinceDay);
            writer.Write(m_UntilDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("imprisoned.future", "Imprisoned read at version " + version + "; fields beyond the current contract were skipped.");
            }
            reader.Read(out m_SinceDay);
            reader.Read(out m_UntilDay);
        }
    }

    public struct SoulPet : IComponentData, ISerializable
    {
        public const byte KindDog = 1;
        public const byte KindCat = 2;
        public const byte KindBird = 3;
        public const byte BondMax = 255;

        public byte m_Kind;            // F47: 0 none, else Kind*
        public byte m_Bond;            // 0..255
        public uint m_AdoptDay;
        public uint m_PetDay;          // daily cursor — PetSystem
        public uint m_LastComfortDay;  // comfort-pulse cursor

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Kind);
            writer.Write(m_Bond);
            writer.Write(m_AdoptDay);
            writer.Write(m_PetDay);
            writer.Write(m_LastComfortDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("soulpet.future", "SoulPet read at version " + version + "; fields beyond the current contract were skipped.");
            }
            reader.Read(out m_Kind);
            reader.Read(out m_Bond);
            reader.Read(out m_AdoptDay);
            reader.Read(out m_PetDay);
            reader.Read(out m_LastComfortDay);
        }
    }

    /// <summary>
    /// Shared M3 gates. Homebound() is the F13/F35 hook the social systems consult;
    /// Scaffold() is the lazy migration for saves created before M3 existed.
    /// </summary>
    public static class SoulMindGate
    {
        public static bool Homebound(EntityManager em, Entity e)
        {
            if (em.HasComponent<SoulMind>(e))
            {
                SoulMind mind = em.GetComponentData<SoulMind>(e);
                if ((mind.m_Flags & SoulMind.FlagWithdrawn) != 0) return true;
            }
            if (em.HasComponent<Imprisoned>(e) && em.IsComponentEnabled<Imprisoned>(e)) return true;
            return false;
        }

        public static void Scaffold(EntityManager em, Entity e)
        {
            if (em.HasComponent<SoulMind>(e)) return;
            em.AddComponentData(e, default(SoulMind));
        }
    }
}
