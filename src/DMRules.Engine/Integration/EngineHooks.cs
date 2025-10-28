using System;

namespace DMRules.Engine.Integration
{
    /// <summary>
    /// エンジン側のライフサイクルにフックするための最小イベント集。
    /// 必要に応じてイベントを増やしてください（後方互換重視）。
    /// </summary>
    public static class EngineHooks
    {
        public static bool Enabled { get; set; } = true;

        public static event Action<object>? OnStateChanged;
        public static event Action<object>? OnBeforeSBA;
        public static event Action<object>? OnAfterSBA;
        public static event Action<object, object?>? OnTriggerQueued;
        public static event Action<string>? OnTrace;

        public static void RaiseStateChanged(object state) { if (Enabled) OnStateChanged?.Invoke(state); }
        public static void RaiseBeforeSBA(object state)   { if (Enabled) OnBeforeSBA?.Invoke(state); }
        public static void RaiseAfterSBA(object state)    { if (Enabled) OnAfterSBA?.Invoke(state); }
        public static void RaiseTriggerQueued(object state, object? trigger)
        { if (Enabled) OnTriggerQueued?.Invoke(state, trigger); }
        public static void Trace(string message) { if (Enabled) OnTrace?.Invoke(message); }

        public static void Reset()
        {
            OnStateChanged = null;
            OnBeforeSBA = null;
            OnAfterSBA = null;
            OnTriggerQueued = null;
            OnTrace = null;
            Enabled = true;
        }
    }
}

// 後方互換: 既存コードが DuelMasters.Engine.Integration.* を参照してもビルド可能にするラッパー
namespace DuelMasters.Engine.Integration
{
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
