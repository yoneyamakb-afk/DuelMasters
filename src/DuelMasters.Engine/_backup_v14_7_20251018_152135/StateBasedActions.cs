
using System;
using System.Collections.Generic;

namespace DuelMasters.Engine;

/// <summary>
/// State-based actions framework. Each rule examines the state and either returns the same state
/// or a modified state. The engine applies rules until no changes occur (fixed point).
/// </summary>
public interface IStateRule
{
    GameState Apply(GameState s);
}

public static class StateBasedActions
{
    // Registry of rules; order matters when dependencies exist.
    private static readonly List<IStateRule> _rules = new()
    {
        // Add concrete rules here in order
        new DestroyZeroPowerRule(),
        // new HandSizeLimitRule(),
    };

    public static GameState Fix(GameState s)
    {
        while (true)
        {
            var before = s;
            foreach (var rule in _rules)
                s = rule.Apply(s);
            if (ReferenceEquals(before, s) || before.Equals(s))
                return s;
        }
    }
}


