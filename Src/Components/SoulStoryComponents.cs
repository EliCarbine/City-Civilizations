using Colossal.Serialization.Entities;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// M4 (F43/F58): one remembered passing. Lives on the registry singleton entity as a
    /// capped ring buffer. SoulDiary.Eulogy renders the line; the detector keeps the vigil.
    /// </summary>
    public struct SoulPassing : IBufferElementData, ISerializable
    {
        public int m_Index;       // entity index of the departed
        public int m_Version;     // version at departure (index-reuse guard)
        public Entity m_Detector; // the first soul to notice
        public uint m_Day;        // day the passing was found
        public byte m_Mourners;   // souls whose edges to them were orphaned
        public byte m_Memories;   // most memories any single mourner carried

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Index);
            writer.Write(m_Version);
            writer.Write(m_Detector);
            writer.Write(m_Day);
            writer.Write(m_Mourners);
            writer.Write(m_Memories);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("passing.future", "SoulPassing read at version " + version + "; fields beyond the current contract were skipped.");
            }
            reader.Read(out m_Index);
            reader.Read(out m_Version);
            reader.Read(out m_Detector);
            reader.Read(out m_Day);
            reader.Read(out m_Mourners);
            reader.Read(out m_Memories);
        }
    }
}
