
namespace DMRules.Effects;

/// <summary>Minimal intermediate representation (IR) for card effects (M12 v0+).</summary>
public static class EffectIR
{
    public abstract record EffectClause;

    public sealed record OnEvent(Trigger Trigger, ActionDef Action) : EffectClause;

    public enum Trigger
    {
        None = 0,
        /// <summary>Triggered when the creature is put into the battle zone (a.k.a. "summon").</summary>
        EnterBattlezone,
        /// <summary>When this is destroyed.</summary>
        Destroyed,
        /// <summary>When this attacks.</summary>
        AttackDeclared,
        /// <summary>When this wins a battle.</summary>
        WinsBattle,
        /// <summary>When this breaks shields (one or more).</summary>
        ShieldBreak
    }

    public abstract record ActionDef;

    public sealed record Draw(int Cards) : ActionDef;

    public sealed record AddMana(int Cards) : ActionDef;

    public sealed record NoOp(string Reason) : ActionDef;

    /// <summary>A complete effect consists of zero or more clauses.</summary>
    public sealed record Effect(EffectClause[] Clauses)
    {
        public static readonly Effect Empty = new([]);
    }
}
