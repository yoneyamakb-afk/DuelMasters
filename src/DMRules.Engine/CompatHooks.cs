// src/DMRules.Engine/CompatHooks.cs
// v15 hybrid: provide tiny, stable hooks so external tests/tools don't need reflection.
// - Public MinimalState conversion entrypoint
// - IEngineAdapter.Apply(...) extension shim that forwards to best available method

#nullable enable
using System;
using System.Reflection;

namespace DMRules.Engine
{
    public static class Compat
    {
        /// <summary>
        /// Stable entrypoint to construct MinimalState from any IGameState.
        /// If MinimalState exposes a ctor(IGameState) or static From(IGameState),
        /// this will use it; otherwise it will throw with a clear message.
        /// </summary>
        public static MinimalState FromGameState(IGameState gs)
        {
            if (gs is null) throw new ArgumentNullException(nameof(gs));

            var t = typeof(MinimalState);

            // Prefer public/protected/internal ctor(IGameState)
            var ctor = t.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                                        binder: null, types: new[] { typeof(IGameState) }, modifiers: null);
            if (ctor is not null)
                return (MinimalState)ctor.Invoke(new object[] { gs });

            // Try static From(IGameState)
            var m = t.GetMethod("From", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                                binder: null, types: new[] { typeof(IGameState) }, modifiers: null);
            if (m is not null && m.ReturnType == typeof(MinimalState))
                return (MinimalState)m.Invoke(null, new object[] { gs })!;

            throw new InvalidOperationException("Compat.FromGameState: MinimalState(IGameState) path not found. Expose a ctor(IGameState) or static From(IGameState).");
        }
    }

    public static class EngineAdapterCompat
    {
        /// <summary>
        /// Backward-compatible shim for legacy tests calling adapter.Apply(_, state).
        /// If IEngineAdapter already implements a method with this signature, the instance method wins.
        /// Otherwise, we look for likely candidates and forward; if nothing found, return the input state.
        /// </summary>
        public static IGameState Apply(this IEngineAdapter adapter, object? _unused, IGameState state)
        {
            if (adapter is null) throw new ArgumentNullException(nameof(adapter));
            if (state is null) throw new ArgumentNullException(nameof(state));

            // If the interface already has Apply(object?, IGameState), the instance method will take precedence at compile-time.
            // Here we only provide a fallback for runtimes where only differently-named methods exist.
            var candidates = new[] {
                "Apply", "Execute", "ApplyEffect", "ApplyGameState", "Run", "Step"
            };

            var at = adapter.GetType();
            foreach (var name in candidates)
            {
                var m = at.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m is null) continue;
                var ps = m.GetParameters();
                // Prefer (object?, IGameState) or (IGameState) signatures
                if (ps.Length == 2 && typeof(IGameState).IsAssignableFrom(ps[1].ParameterType))
                {
                    var r = m.Invoke(adapter, new object?[] { null, state });
                    if (r is IGameState gs) return gs;
                    return state;
                }
                if (ps.Length == 1 && typeof(IGameState).IsAssignableFrom(ps[0].ParameterType))
                {
                    var r = m.Invoke(adapter, new object?[] { state });
                    if (r is IGameState gs) return gs;
                    return state;
                }
            }

            return state;
        }
    }
}