#nullable enable
using System;
using System.Linq;
using System.Reflection;
using DMRules.Engine;

namespace DMRules.Tests;

public static class StaticCompat
{
    private static readonly Assembly EngineAsm = typeof(IGameState).Assembly;

    private static Type RequireType(string n)
    {
        var t = EngineAsm.GetTypes().FirstOrDefault(x =>
            x.Name == n || x.FullName?.EndsWith("." + n) == true);
        if (t == null) throw new InvalidOperationException($"Compat: type not found: {n}");
        return t;
    }

    private static object ParseEnum(Type enumType, string value)
    {
        if (!Enum.TryParse(enumType, value, true, out var parsed))
            throw new ArgumentException($"Compat: cannot parse '{value}' as {enumType.Name}");
        return parsed!;
    }

    private static MethodInfo RequireStep(params Type[][] signatures)
    {
        foreach (var sig in signatures)
        {
            var mi = EngineAsm.GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .FirstOrDefault(m =>
                {
                    if (m.Name != "Step") return false;
                    var ps = m.GetParameters();
                    if (ps.Length != sig.Length) return false;
                    for (int i = 0; i < ps.Length; i++)
                        if (!ps[i].ParameterType.IsAssignableFrom(sig[i])) return false;
                    return typeof(IGameState).IsAssignableFrom(m.ReturnType);
                });
            if (mi != null) return mi;
        }
        throw new MissingMethodException("Compat: Step overload not found");
    }

    private static MethodInfo RequireApplyReplacement()
    {
        var mi = EngineAsm.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .FirstOrDefault(m =>
            {
                if (m.Name != "ApplyReplacement") return false;
                var ps = m.GetParameters();
                if (ps.Length != 2) return false;
                if (!typeof(IGameState).IsAssignableFrom(ps[0].ParameterType)) return false;
                return typeof(IGameState).IsAssignableFrom(m.ReturnType);
            });
        if (mi == null) throw new MissingMethodException("Compat: ApplyReplacement not found");
        return mi;
    }

    public static MinimalState Step(MinimalState s, string phase)
    {
        var PhaseT = RequireType("Phase");
        var p = ParseEnum(PhaseT, phase);
        var step = RequireStep(new[] { typeof(IGameState), PhaseT });
        var r = step.Invoke(null, new object[] { (IGameState)s, p });
        return (MinimalState)r;
    }

    public static MinimalState Step(MinimalState s, string phase, string priority)
    {
        var PhaseT = RequireType("Phase");
        var PriorityT = RequireType("Priority");
        var p = ParseEnum(PhaseT, phase);
        var q = ParseEnum(PriorityT, priority);
        var step = RequireStep(
            new[] { typeof(IGameState), PhaseT, PriorityT },
            new[] { typeof(IGameState), PriorityT, PhaseT });
        var r = step.Invoke(null, new object[] { (IGameState)s, p, q });
        return (MinimalState)r;
    }

    public static MinimalState ApplyReplacement(MinimalState s, object replacement)
    {
        var apply = RequireApplyReplacement();
        var r = apply.Invoke(null, new object[] { (IGameState)s, replacement });
        return (MinimalState)r;
    }
}
