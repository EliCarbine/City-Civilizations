// Compile-check stubs: Unity.Collections surface used by Citizen Soul.
// Mirrors the real Unity Collections signatures; bodies are inert (never executed).

namespace Unity.Collections
{
    public enum Allocator
    {
        Invalid = 0,
        None = 1,
        Temp = 2,
        TempJob = 3,
        Persistent = 4
    }

    public struct NativeArray<T> where T : struct
    {
        public int Length { get { return 0; } }
        public bool IsCreated { get { return false; } }

        public T this[int index]
        {
            get { return default(T); }
            set { }
        }

        public void Dispose() { }

        public Enumerator GetEnumerator() { return default(Enumerator); }

        public struct Enumerator
        {
            public T Current { get { return default(T); } }
            public bool MoveNext() { return false; }
        }
    }

    public struct NativeHashMap<TKey, TValue>
        where TKey : unmanaged, System.IEquatable<TKey>
        where TValue : unmanaged
    {
        public NativeHashMap(int initialCapacity, Allocator allocator) { }

        public int Count { get { return 0; } }
        public bool IsCreated { get { return false; } }

        public bool TryAdd(TKey key, TValue value) { return false; }
        public bool TryGetValue(TKey key, out TValue value) { value = default(TValue); return false; }
        public bool ContainsKey(TKey key) { return false; }
        public bool Remove(TKey key) { return false; }
        public void Clear() { }
        public void Dispose() { }
    }
}
