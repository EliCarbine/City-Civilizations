// Compile-check stubs: UI surface (Colossal.UI.Binding + Game.UI) used by Citizen Soul.
// Modeled from public CS2 UI-modding documentation; real-assembly verification is
// scheduled for M6 QA on a machine with the game installed.

using Unity.Entities;

namespace Colossal.UI.Binding
{
    /// <summary>Base for all UI bindings.</summary>
    public abstract class BindingBase
    {
    }

    /// <summary>Pushes a value to the UI; call Update() when it changes.</summary>
    public class ValueBinding<T> : BindingBase
    {
        public ValueBinding(string group, string name, T initialValue) { }

        public void Update(T value) { }
    }

    /// <summary>Raises a named one-way event from the UI into the game.</summary>
    public class TriggerBinding : BindingBase
    {
        public TriggerBinding(string group, string name, System.Action handler) { }
    }
}

namespace Game.UI
{
    /// <summary>Base class for mod UI systems; registered via UpdateSystem.</summary>
    public abstract class UISystemBase : GameSystemBase
    {
        protected void AddBinding(Colossal.UI.Binding.BindingBase binding) { }
    }
}
