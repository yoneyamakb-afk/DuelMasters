
// src/DMRules.Engine/M16/ShieldTriggerRules.cs
// Minimal window for ShieldTrigger: called after Break -> Reveal -> (Trigger) -> stack/cast.
using System.Collections.Generic;

namespace DMRules.Engine.M16
{
    public static class ShieldTriggerRules
    {
        public sealed class ShieldReveal
        {
            public string CardName { get; }
            public bool IsTrigger { get; }
            public ShieldReveal(string cardName, bool isTrigger) { CardName = cardName; IsTrigger = isTrigger; }
        }

        public static IEnumerable<string> ResolveShieldWindow(IEnumerable<ShieldReveal> reveals)
        {
            // APNAP order is assumed to be handled by the host engine.
            foreach (var r in reveals)
            {
                if (r.IsTrigger)
                    yield return $"TRIGGER:{r.CardName}";
            }
        }
    }
}
