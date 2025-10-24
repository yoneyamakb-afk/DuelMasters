
using System.Text.RegularExpressions;

namespace DMRules.Effects;

/// <summary>
/// Parser for the constrained pseudo-DSL plus JP normalization (M12.6+).
/// M12.7: 追加トリガー（destroyed / battle win / shield break）と
/// アクション省略時（NoOp）を認識。
/// </summary>
public static class EffectParser
{
    private static readonly Regex OnSummonDraw =
        new(@"^\s*on\s+(summon|enter)\s*:\s*draw\s+(\d+)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex OnAttackDraw =
        new(@"^\s*on\s+attack\s*:\s*draw\s+(\d+)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Bare triggers without action (→ NoOp)
    private static readonly Regex BareSummon =
        new(@"^\s*on\s+(summon|enter)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex BareAttack =
        new(@"^\s*on\s+attack\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex BareDestroyed =
        new(@"^\s*on\s+destroyed\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex BareBattleWin =
        new(@"^\s*on\s+battle\s+win\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex BareShieldBreak =
        new(@"^\s*on\s+shield\s+break\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static EffectIR.Effect Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return EffectIR.Effect.Empty;

        var normalized = EffectTextNormalizer.NormalizeToDsl(text);
        if (!string.IsNullOrEmpty(normalized))
        {
            text = normalized;
        }

        text = text.Trim();

        // on summon/enter: draw N
        var m1 = OnSummonDraw.Match(text);
        if (m1.Success && int.TryParse(m1.Groups[2].Value, out var n1))
        {
            return new EffectIR.Effect(
                [ new EffectIR.OnEvent(EffectIR.Trigger.EnterBattlezone, new EffectIR.Draw(n1)) ]
            );
        }

        // on attack: draw N
        var m2 = OnAttackDraw.Match(text);
        if (m2.Success && int.TryParse(m2.Groups[1].Value, out var n2))
        {
            return new EffectIR.Effect(
                [ new EffectIR.OnEvent(EffectIR.Trigger.AttackDeclared, new EffectIR.Draw(n2)) ]
            );
        }

        // Bare triggers -> NoOp
        if (BareSummon.IsMatch(text))
            return new EffectIR.Effect([ new EffectIR.OnEvent(EffectIR.Trigger.EnterBattlezone, new EffectIR.NoOp("no action")) ]);
        if (BareAttack.IsMatch(text))
            return new EffectIR.Effect([ new EffectIR.OnEvent(EffectIR.Trigger.AttackDeclared, new EffectIR.NoOp("no action")) ]);
        if (BareDestroyed.IsMatch(text))
            return new EffectIR.Effect([ new EffectIR.OnEvent(EffectIR.Trigger.Destroyed, new EffectIR.NoOp("no action")) ]);
        if (BareBattleWin.IsMatch(text))
            return new EffectIR.Effect([ new EffectIR.OnEvent(EffectIR.Trigger.WinsBattle, new EffectIR.NoOp("no action")) ]);
        if (BareShieldBreak.IsMatch(text))
            return new EffectIR.Effect([ new EffectIR.OnEvent(EffectIR.Trigger.ShieldBreak, new EffectIR.NoOp("no action")) ]);

        // Unknown
        return EffectIR.Effect.Empty;
    }
}
