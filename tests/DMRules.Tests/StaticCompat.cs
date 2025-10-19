#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using DMRules.Engine;

namespace DMRules.Tests;

public static class StaticCompat
{
    private static readonly Assembly EngineAsm = typeof(IGameState).Assembly;

    private sealed class Invoker
    {
        public required MethodInfo Method;
        public object? Target; // null for static; non-null for instance
        public bool ReturnsState; // true if method itself returns a state
        public bool IsInstanceSetPhase; // true if fallback SetPhase(instance) used
    }

    private static Type RequireType(string simpleOrFullName)
    {
        var t = EngineAsm.GetTypes().FirstOrDefault(x =>
            x.Name == simpleOrFullName || x.FullName?.EndsWith("." + simpleOrFullName) == true);
        if (t == null) throw new InvalidOperationException($"Compat: type not found: {simpleOrFullName} in {EngineAsm.FullName}");
        return t;
    }

    private static object ParseEnum(Type enumType, string value)
    {
        if (!Enum.TryParse(enumType, value, ignoreCase: true, out var parsed))
            throw new ArgumentException($"Compat: cannot parse '{value}' as {enumType.Name}");
        return parsed!;
    }

    private static IEnumerable<Invoker> EnumerateCandidateMethods(string[] preferredNames)
    {
        foreach (var t in EngineAsm.GetTypes())
        {
            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                yield return new Invoker { Method = m, Target = null, ReturnsState = LooksLikeState(m.ReturnType) };
            }

            var singleton = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (singleton is not null && singleton.CanRead)
            {
                object? inst = null;
                try { inst = singleton.GetValue(null); } catch { /* ignore */ }
                if (inst is not null)
                {
                    foreach (var m in inst.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
                    {
                        yield return new Invoker { Method = m, Target = inst, ReturnsState = LooksLikeState(m.ReturnType) };
                    }
                }
            }
        }
    }

    private static bool LooksLikeState(Type ret)
    {
        return typeof(IGameState).IsAssignableFrom(ret) || ret.Name.Contains("MinimalState", StringComparison.Ordinal);
    }

    private static bool ParamCompat(Type have, Type want)
    {
        return have == want || have.IsAssignableFrom(want) || want.IsAssignableFrom(have);
    }

    private static Invoker? TryRequireStep(params Type[][] signatures)
    {
        string[] names = new[] { "Step", "Advance", "AdvancePhase", "Next", "ProcessPhase", "DoStep" };

        var candidates = EnumerateCandidateMethods(names)
            .Where(inv => names.Contains(inv.Method.Name) && inv.ReturnsState)
            .ToArray();

        foreach (var sig in signatures)
        {
            foreach (var inv in candidates)
            {
                var ps = inv.Method.GetParameters();
                if (ps.Length != sig.Length) continue;

                bool ok = true;
                for (int i = 0; i < ps.Length; i++)
                {
                    if (!ParamCompat(ps[i].ParameterType, sig[i])) { ok = false; break; }
                }
                if (ok) return inv;
            }
        }

        return null;
    }

    private static Invoker? TryRequireSetPhaseOnInstance(object stateInstance, Type phaseType)
    {
        var t = stateInstance.GetType();
        var m = t.GetMethod("SetPhase", BindingFlags.Public | BindingFlags.Instance, new Type[] { phaseType });
        if (m == null) return null;

        return new Invoker
        {
            Method = m,
            Target = stateInstance,
            ReturnsState = false,
            IsInstanceSetPhase = true
        };
    }

    private static Invoker RequireApplyReplacement()
    {
        foreach (var inv in EnumerateCandidateMethods(new[] { "ApplyReplacement" }))
        {
            if (inv.Method.Name != "ApplyReplacement") continue;
            var ps = inv.Method.GetParameters();
            if (ps.Length != 2) continue;
            if (!(typeof(IGameState).IsAssignableFrom(ps[0].ParameterType) || ps[0].ParameterType.Name.Contains("MinimalState"))) continue;
            if (!LooksLikeState(inv.Method.ReturnType)) continue;
            return inv;
        }
        throw new MissingMethodException("Compat: ApplyReplacement not found");
    }

    public static MinimalState Step(MinimalState s, string phase)
    {
        var PhaseT = RequireType("Phase");
        var p = ParseEnum(PhaseT, phase);

        var inv = TryRequireStep(new[] { typeof(IGameState), PhaseT }, new[] { typeof(MinimalState), PhaseT });
        if (inv is null)
        {
            inv = TryRequireSetPhaseOnInstance(s, PhaseT);
        }
        if (inv is null)
        {
            throw new MissingMethodException("Compat: Step/Advance/SetPhase not found");
        }

        object? result;
        if (inv.IsInstanceSetPhase)
        {
            inv.Method.Invoke(inv.Target, new object[] { p });
            result = s;
        }
        else
        {
            result = inv.Method.Invoke(inv.Target, new object[] { (IGameState)s, p });
        }
        return (MinimalState)result!;
    }

    public static MinimalState Step(MinimalState s, string phase, string priority)
    {
        var PhaseT = RequireType("Phase");
        var PriorityT = RequireType("Priority");
        var p = ParseEnum(PhaseT, phase);
        var q = ParseEnum(PriorityT, priority);

        var inv = TryRequireStep(
            new[] { typeof(IGameState), PhaseT, PriorityT },
            new[] { typeof(IGameState), PriorityT, PhaseT },
            new[] { typeof(MinimalState), PhaseT, PriorityT },
            new[] { typeof(MinimalState), PriorityT, PhaseT }
        );

        if (inv is null)
        {
            return Step(s, phase);
        }

        var result = inv.Method.Invoke(inv.Target, new object[] { (IGameState)s, p, q });
        return (MinimalState)result!;
    }

    public static MinimalState ApplyReplacement(MinimalState s, object replacement)
    {
        var inv = RequireApplyReplacement();
        var result = inv.Method.Invoke(inv.Target, new object[] { (IGameState)s, replacement });
        return (MinimalState)result!;
    }
}
