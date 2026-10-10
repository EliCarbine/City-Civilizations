// Compile-check stubs: Unity.Mathematics surface used by Citizen Soul.
// Mirrors the real signatures; bodies are inert (never executed).

namespace Unity.Mathematics
{
    public struct Random
    {
        public uint state;

        public Random(uint seed)
        {
            state = seed == 0u ? 1u : seed;
        }

        public static Random CreateFromIndex(uint index)
        {
            return new Random(index);
        }

        public int NextInt() { return 0; }
        public int NextInt(int max) { return 0; }
        public int NextInt(int min, int max) { return min; }
        public uint NextUInt() { return 0u; }
        public uint NextUInt(uint max) { return 0u; }
        public float NextFloat() { return 0f; }
        public float NextFloat(float min, float max) { return min; }
        public bool NextBool() { return false; }
    }
}
