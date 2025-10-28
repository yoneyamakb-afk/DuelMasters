// Final M13: DuelMasters.Engine side compatibility layer for legacy references.
// Place at: src/DuelMasters.Engine/Integration/Compat.cs
namespace DuelMasters.Engine.Integration
{
    public static class EngineWireup
    {
        public static void Configure(string? traceOutputDir = null)
            => DMRules.Engine.Integration.EngineWireup.Configure(traceOutputDir);
    }

    public static class EngineHooks
    {
        public static bool Enabled
        {
            get => DMRules.Engine.Integration.EngineHooks.Enabled;
            set => DMRules.Engine.Integration.EngineHooks.Enabled = value;
        }

        public static event System.Action<object>? OnStateChanged
        {
            add { DMRules.Engine.Integration.EngineHooks.OnStateChanged += value; }
            remove { DMRules.Engine.Integration.EngineHooks.OnStateChanged -= value; }
        }
        public static event System.Action<object>? OnBeforeSBA
        {
            add { DMRules.Engine.Integration.EngineHooks.OnBeforeSBA += value; }
            remove { DMRules.Engine.Integration.EngineHooks.OnBeforeSBA -= value; }
        }
        public static event System.Action<object>? OnAfterSBA
        {
            add { DMRules.Engine.Integration.EngineHooks.OnAfterSBA += value; }
            remove { DMRules.Engine.Integration.EngineHooks.OnAfterSBA -= value; }
        }
        public static event System.Action<object, object?>? OnTriggerQueued
        {
            add { DMRules.Engine.Integration.EngineHooks.OnTriggerQueued += value; }
            remove { DMRules.Engine.Integration.EngineHooks.OnTriggerQueued -= value; }
        }
        public static event System.Action<string>? OnTrace
        {
            add { DMRules.Engine.Integration.EngineHooks.OnTrace += value; }
            remove { DMRules.Engine.Integration.EngineHooks.OnTrace -= value; }
        }

        public static void RaiseStateChanged(object s) => DMRules.Engine.Integration.EngineHooks.RaiseStateChanged(s);
        public static void RaiseBeforeSBA(object s)    => DMRules.Engine.Integration.EngineHooks.RaiseBeforeSBA(s);
        public static void RaiseAfterSBA(object s)     => DMRules.Engine.Integration.EngineHooks.RaiseAfterSBA(s);
        public static void RaiseTriggerQueued(object s, object? t)
            => DMRules.Engine.Integration.EngineHooks.RaiseTriggerQueued(s, t);
        public static void Trace(string msg) => DMRules.Engine.Integration.EngineHooks.Trace(msg);
        public static void Reset() => DMRules.Engine.Integration.EngineHooks.Reset();
    }
}
