// Compile-check stubs: Unity.Entities surface used by Citizen Soul.
// Mirrors the real Unity ECS signatures; bodies are inert (never executed).

using System;
using Unity.Collections;

namespace Unity.Entities
{
    public struct Entity : IEquatable<Entity>
    {
        public int Index;
        public int Version;

        public static Entity Null { get { return default(Entity); } }

        public bool Equals(Entity other) { return Index == other.Index && Version == other.Version; }
        public override bool Equals(object obj) { return obj is Entity && Equals((Entity)obj); }
        public override int GetHashCode() { return Index ^ (Version << 16); }
        public static bool operator ==(Entity a, Entity b) { return a.Equals(b); }
        public static bool operator !=(Entity a, Entity b) { return !a.Equals(b); }
        public override string ToString() { return "Entity(" + Index + ":" + Version + ")"; }
    }

    public interface IComponentData { }
    public interface IBufferElementData { }
    public interface IEnableableComponent { }

    public struct EntityArchetype
    {
    }

    public struct ComponentType
    {
        public static ComponentType ReadOnly<T>() { return default(ComponentType); }
        public static ComponentType ReadWrite<T>() { return default(ComponentType); }
        public static ComponentType Exclude<T>() { return default(ComponentType); }
        public static ComponentType ReadOnly(Type type) { return default(ComponentType); }
        public static ComponentType ReadWrite(Type type) { return default(ComponentType); }
        public static ComponentType Exclude(Type type) { return default(ComponentType); }
    }

    public enum EntityQueryOptions
    {
        Default = 0,
        IncludeDisabled = 1,
        IgnoreComponentEnabledState = 2
    }

    public class EntityQueryDesc
    {
        public ComponentType[] All;
        public ComponentType[] Any;
        public ComponentType[] None;
        public EntityQueryOptions Options;
    }

    public struct DynamicBuffer<T> where T : struct
    {
        public int Length { get { return 0; } }
        public int Capacity { get { return 0; } }

        public T this[int index]
        {
            get { return default(T); }
            set { }
        }

        public void Add(T element) { }
        public void Insert(int index, T element) { }
        public void RemoveAt(int index) { }
        public void Clear() { }
        public void EnsureCapacity(int capacity) { }
        public void TrimExcess() { }

        public Enumerator GetEnumerator() { return default(Enumerator); }

        public struct Enumerator
        {
            public T Current { get { return default(T); } }
            public bool MoveNext() { return false; }
        }
    }

    public class EntityQuery
    {
        public NativeArray<Entity> ToEntityArray(Allocator allocator) { return default(NativeArray<Entity>); }
        public NativeArray<T> ToComponentDataArray<T>(Allocator allocator) where T : struct { return default(NativeArray<T>); }
        public Entity GetSingletonEntity() { return Entity.Null; }
        public T GetSingleton<T>() where T : struct { return default(T); }
        public bool HasSingleton<T>() where T : struct { return false; }
        public int CalculateEntityCount() { return 0; }
        public bool IsEmpty { get { return true; } }
        public void Dispose() { }
    }

    public class EntityManager
    {
        public bool Exists(Entity entity) { return false; }
        public bool HasComponent<T>(Entity entity) where T : struct { return false; }
        public T GetComponentData<T>(Entity entity) where T : struct { return default(T); }
        public void SetComponentData<T>(Entity entity, T value) where T : struct { }
        public void AddComponent<T>(Entity entity) where T : struct { }
        public void AddComponentData<T>(Entity entity, T value) where T : struct { }
        public bool RemoveComponent<T>(Entity entity) where T : struct { return true; }
        public DynamicBuffer<T> GetBuffer<T>(Entity entity) where T : struct { return default(DynamicBuffer<T>); }
        public DynamicBuffer<T> AddBuffer<T>(Entity entity) where T : struct { return default(DynamicBuffer<T>); }
        public bool IsComponentEnabled<T>(Entity entity) where T : struct { return false; }
        public void SetComponentEnabled<T>(Entity entity, bool value) where T : struct { }
        public Entity CreateEntity() { return Entity.Null; }
        public Entity CreateEntity(EntityArchetype archetype) { return Entity.Null; }
        public void DestroyEntity(Entity entity) { }
        public T GetSingleton<T>() where T : struct { return default(T); }
        public Entity GetSingletonEntity<T>() where T : struct { return Entity.Null; }
    }

    public class World
    {
        public T GetOrCreateSystemManaged<T>() where T : ComponentSystemBase
        {
            return (T)Activator.CreateInstance(typeof(T));
        }

        public T GetExistingSystemManaged<T>() where T : ComponentSystemBase { return null; }
    }

    public abstract class ComponentSystemBase
    {
        public World World { get; internal set; }
        public EntityManager EntityManager { get; internal set; }

        protected virtual void OnCreate() { }
        protected virtual void OnStartRunning() { }
        protected virtual void OnStopRunning() { }
        protected virtual void OnDestroy() { }
        protected virtual void OnUpdate() { }
    }

    public abstract class SystemBase : ComponentSystemBase
    {
        protected EntityQuery GetEntityQuery(params ComponentType[] componentTypes) { return new EntityQuery(); }
        protected EntityQuery GetEntityQuery(EntityQueryDesc desc) { return new EntityQuery(); }
        protected EntityQuery GetEntityQuery(params EntityQueryDesc[] descs) { return new EntityQuery(); }
        protected void RequireForUpdate(EntityQuery query) { }
    }
}
