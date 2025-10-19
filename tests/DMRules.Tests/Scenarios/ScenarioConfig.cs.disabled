#nullable enable
using System;
using System.Linq;
using System.Reflection;
using DMRules.Engine;

namespace DMRules.Tests.Scenarios;

public static class ScenarioConfig
{
    public static MinimalState CreateInitialState()
    {
        var t = typeof(MinimalState);
        var ctor = t.GetConstructor(Type.EmptyTypes);
        if (ctor != null) return (MinimalState)ctor.Invoke(Array.Empty<object?>());
        ctor = t.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(ci => ci.GetParameters().Length == 0);
        if (ctor != null) return (MinimalState)ctor.Invoke(Array.Empty<object?>());
        var factory = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .SelectMany(tp => tp.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            .FirstOrDefault(m =>
                (typeof(IGameState).IsAssignableFrom(m.ReturnType) || typeof(MinimalState).IsAssignableFrom(m.ReturnType)) &&
                m.GetParameters().Length == 0);
        if (factory != null)
        {
            var state = factory.Invoke(null, Array.Empty<object?>());
            return (MinimalState)state!;
        }
        throw new InvalidOperationException("Could not construct MinimalState for scenarios.");
    }

    public static string[] GetPhaseNames()
    {
        var asm = typeof(IGameState).Assembly;
        var phaseT = asm.GetTypes().FirstOrDefault(x => x.Name == "Phase" || x.FullName?.EndsWith(".Phase") == true);
        if (phaseT == null || !phaseT.IsEnum) return Array.Empty<string>();
        return Enum.GetNames(phaseT);
    }

    public static string[] GetPriorityNames()
    {
        var asm = typeof(IGameState).Assembly;
        var prT = asm.GetTypes().FirstOrDefault(x => x.Name == "Priority" || x.FullName?.EndsWith(".Priority") == true);
        if (prT == null || !prT.IsEnum) return Array.Empty<string>();
        return Enum.GetNames(prT);
    }
}
