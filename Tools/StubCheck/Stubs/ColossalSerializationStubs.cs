// Compile-check stubs: Colossal.Serialization.Entities surface used by Citizen Soul.
// Mirrors the real CS2 serialization signatures; bodies are inert (never executed).

using Unity.Entities;

namespace Colossal.Serialization.Entities
{
    public interface ISerializable
    {
        void Serialize<TWriter>(TWriter writer) where TWriter : IWriter;
        void Deserialize<TReader>(TReader reader) where TReader : IReader;
    }

    public interface IWriter
    {
        void Write(bool value);
        void Write(byte value);
        void Write(sbyte value);
        void Write(short value);
        void Write(ushort value);
        void Write(int value);
        void Write(uint value);
        void Write(long value);
        void Write(ulong value);
        void Write(float value);
        void Write(double value);
        void Write(Entity value);
    }

    public interface IReader
    {
        Context context { get; }

        void Read(out bool value);
        void Read(out byte value);
        void Read(out sbyte value);
        void Read(out short value);
        void Read(out ushort value);
        void Read(out int value);
        void Read(out uint value);
        void Read(out long value);
        void Read(out ulong value);
        void Read(out float value);
        void Read(out double value);
        void Read(out Entity value);
    }

    public struct Context
    {
        public uint version;
        public uint length;
    }
}
