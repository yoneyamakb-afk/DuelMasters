// M15.2d - Card Text Template Dictionary (Auto-Add Top Patterns)
// This file REPLACES CardTextTemplates.cs (apply via Apply_M15_2d.ps1).
// - Keeps all fixes up to M15.1l (GZero priority, trigger ordering, ZERO precedence)
// - Adds new TemplateKeys & patterns for unresolved Top patterns (unblockable, slayer, etc.)
// - Broadens S/W trigger variants with optional explanatory parentheses.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DMRules.Engine.TextParsing
{
    public enum TemplateKey
    {
        // ===== Base =====
        OptionalAction,
        EachTimeEvent,
        ReplacementEffect,
        UntilEndOfTurn,
        DuringNextOppTurn,
        DuringNextSelfTurn,
        MaxN,
        AnyOne,
        Random,
        TwinImpact,
        HyperMode,
        ShieldTrigger,
        CostChange,
        ReplacementPrevention,

        // ===== DM core =====
        DDD,
        GStrike,
        WBreaker,
        TBreaker,
        WorldBreaker,
        GNeoEvo,
        NeoEvo,
        KakumeiChange,
        Charger,

        // ===== Ignored markers =====
        SetMarker,
        RuleNoteGR,         // （ゲーム開始時、GR～）などの注釈

        // ===== Pack2 / modern =====
        Invasion,
        InvasionZero,
        GZero,
        SpeedAttacker,
        Blocker,
        EXLife,
        Fusigiverse,
        Mekureido,
        Bazurenda,
        ShinkaRush,
        OregaAura,
        OniTime,
        Kakusei,
        KakuseiLink,
        ManaArm,
        ShinkaPower,
        ShinkaRise,
        STrigger,
        DTrigger,
        SuperTrigger,
        CIP,
        WhenAttack,
        WhenBlock,

        // ===== New (Top100対策) =====
        Unblockable,            // このクリーチャーはブロックされない。
        CannotAttackPlayers,    // このクリーチャーは～プレイヤーを攻撃できない。
        Slayer,                 // スレイヤー（説明あり）
        MachFighter,            // マッハファイター
        Hunting,                // ハンティング（説明あり）
        PowerAttacker,          // パワーアタッカー +N
        TappedWhenMana          // マナゾーンに置く時、～タップして置く。
    }

    public sealed class TemplateDef
    {
        public TemplateKey Key { get; }
        public Regex Regex { get; }
        public string Example { get; }
        public bool Ignore { get; }

        public TemplateDef(TemplateKey key, string pattern, string example, bool ignore=false)
        {
            Key = key;
            Regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline);
            Example = example;
            Ignore = ignore;
        }
    }

    public static class CardTextTemplates
    {
        public static readonly Regex SetMarkerStrip = new Regex(@"[【\[]\s*[A-Za-z]{2,4}\d{1,3}\s*[】\]]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Order is significant. Highest priority first when overlaps exist.
        private static readonly List<TemplateDef> _defs = new List<TemplateDef>
        {
            // ===== Priority: GZero first (line-start anchor) =====
            new TemplateDef(TemplateKey.GZero,         @"^G・?ゼロ", "G・ゼロ / Gゼロ"),

            // ===== Base =====
            new TemplateDef(TemplateKey.OptionalAction,
                @"(して|引いて|選んで|使って|破壊して|捨てて|戻して|出して|唱えて|ブレイクして)もよい",
                "～してもよい / ～選んでもよい"),

            new TemplateDef(TemplateKey.EachTimeEvent, @"するたび", "～するたび"),
            new TemplateDef(TemplateKey.ReplacementEffect, @"のかわりに", "～のかわりに"),
            new TemplateDef(TemplateKey.UntilEndOfTurn, @"ターンの終わりまで", "ターンの終わりまで"),
            new TemplateDef(TemplateKey.DuringNextOppTurn, @"相手の(次の)?ターン(中|の間|のはじめまで|の終わりまで)", "相手の次のターン中/のはじめまで"),
            new TemplateDef(TemplateKey.DuringNextSelfTurn, @"自分の(次の)?ターン(中|の間|のはじめまで|の終わりまで)", "自分の次のターンのはじめまで"),
            new TemplateDef(TemplateKey.MaxN, @"最大(?<N>\d+)", "最大2 など"),
            new TemplateDef(TemplateKey.AnyOne, @"いずれかの", "いずれかの～"),
            new TemplateDef(TemplateKey.Random, @"ランダム", "ランダムに～"),
            new TemplateDef(TemplateKey.TwinImpact, @"ツインパクト|双面カード", "ツインパクト/双面カード"),
            new TemplateDef(TemplateKey.HyperMode, @"ハイパーモード|ハイパー化|OVERハイパー化", "ハイパーモード"),

            // Trigger family: specific ones BEFORE ShieldTrigger
            new TemplateDef(TemplateKey.SuperTrigger,  @"スーパー(?:[（(].*?[）)])?・?トリガー", "スーパー・トリガー"),
            new TemplateDef(TemplateKey.STrigger,      @"S(?:[（(].*?[）)])?・?トリガー", "S・トリガー"),
            new TemplateDef(TemplateKey.DTrigger,      @"D(?:[（(].*?[）)])?・?トリガー", "D・トリガー"),
            new TemplateDef(TemplateKey.ShieldTrigger, @"シールド(?:[（(].*?[）)])?・?トリガー", "シールド・トリガー"),
            new TemplateDef(TemplateKey.CostChange,
                @"コスト(を)?\s*(?<M>\d+)?\s*(?<Sign>減らす|増やす|下げる|上げる)|コスト(を)?(?<Sign2>\+|-)\s*(?<M2>\d+)",
                "コストを1減らす / コスト-1 / コストを上げる"),
            new TemplateDef(TemplateKey.ReplacementPrevention, @"するかわりに.*しない", "～するかわりに～しない"),

            // ===== DM core =====
            new TemplateDef(TemplateKey.DDD, @"D・D・D", "D・D・D"),
            new TemplateDef(TemplateKey.GStrike, @"G・ストライク", "G・ストライク"),
            new TemplateDef(TemplateKey.WBreaker, @"W(?:[（(].*?[）)])?・?ブレイカー", "W・ブレイカー / W(ダブル)・ブレイカー"),
            new TemplateDef(TemplateKey.TBreaker, @"T(?:[（(].*?[）)])?・?ブレイカー", "T・ブレイカー"),
            new TemplateDef(TemplateKey.WorldBreaker, @"(ワールド|WORLD)[・･・]?(ブレイカー|BREAKER)", "ワールド・ブレイカー / WORLD・BREAKER"),
            new TemplateDef(TemplateKey.GNeoEvo, @"G-NEO進化", "G-NEO進化"),
            new TemplateDef(TemplateKey.NeoEvo, @"NEO進化", "NEO進化"),
            new TemplateDef(TemplateKey.KakumeiChange, @"革命(チェンジ|\d+)", "革命チェンジ/革命2/革命0"),
            new TemplateDef(TemplateKey.Charger, @"チャージャー[（(][^)]*[）)]", "チャージャー（…）/チャージャー(...)"),

            // Ignored markers
            new TemplateDef(TemplateKey.SetMarker, @"[【\[]\s*[A-Za-z]{2,4}\d{1,3}\s*[】\]]", "[ll03]/【lwn05】 など", ignore:true),
            new TemplateDef(TemplateKey.RuleNoteGR, @"[（(]ゲーム開始時、?\s*GR.*?[）)]", "GR注釈", ignore:true),

            // ===== Pack2 (order-sensitive) =====
            new TemplateDef(TemplateKey.InvasionZero,  @"侵略\s*ZERO", "侵略ZERO"),
            new TemplateDef(TemplateKey.Invasion,      @"侵略(?!\s*ZERO)", "侵略-○○"),
            new TemplateDef(TemplateKey.SpeedAttacker, @"スピード・?アタッカー", "スピードアタッカー"),
            new TemplateDef(TemplateKey.Blocker,       @"ブロッカー", "ブロッカー"),
            new TemplateDef(TemplateKey.EXLife,        @"EX・?ライフ", "EXライフ"),
            new TemplateDef(TemplateKey.Fusigiverse,   @"フシギバース", "フシギバース"),
            new TemplateDef(TemplateKey.Mekureido,     @"メクレイド", "メクレイド"),
            new TemplateDef(TemplateKey.Bazurenda,     @"バズレンダ", "バズレンダ"),
            new TemplateDef(TemplateKey.ShinkaRush,    @"シンカラッシュ", "シンカラッシュ"),
            new TemplateDef(TemplateKey.OregaAura,     @"オレガ・?オーラ", "オレガ・オーラ"),
            new TemplateDef(TemplateKey.OniTime,       @"鬼タイム", "鬼タイム"),
            new TemplateDef(TemplateKey.Kakusei,       @"覚醒(?!リンク)", "覚醒"),
            new TemplateDef(TemplateKey.KakuseiLink,   @"覚醒リンク", "覚醒リンク"),
            new TemplateDef(TemplateKey.ManaArm,       @"マナ武装", "マナ武装"),
            new TemplateDef(TemplateKey.ShinkaPower,   @"シンカパワー", "シンカパワー"),
            new TemplateDef(TemplateKey.ShinkaRise,    @"シンカライズ", "シンカライズ"),
            new TemplateDef(TemplateKey.CIP,           @"出た時", "出た時"),
            new TemplateDef(TemplateKey.WhenAttack,    @"攻撃する時", "攻撃する時"),
            new TemplateDef(TemplateKey.WhenBlock,     @"ブロックした時", "ブロックした時"),

            // ===== New (Top100対策) =====
            new TemplateDef(TemplateKey.Unblockable,          @"このクリーチャーはブロックされない。", "ブロック不可"),
            new TemplateDef(TemplateKey.CannotAttackPlayers,  @"このクリーチャーは.*?プレイヤーを攻撃できない。", "プレイヤー攻撃不可"),
            new TemplateDef(TemplateKey.Slayer,               @"スレイヤー(?:[（(].*?[）)])?", "スレイヤー"),
            new TemplateDef(TemplateKey.MachFighter,          @"マッハファイター", "マッハファイター"),
            new TemplateDef(TemplateKey.Hunting,              @"ハンティング(?:[（(].*?[）)])?", "ハンティング"),
            new TemplateDef(TemplateKey.PowerAttacker,        @"パワーアタッカー\+?\s*(?<N>\d+)", "パワーアタッカー+2000"),
            new TemplateDef(TemplateKey.TappedWhenMana,       @"マナゾーンに置く時.*タップして置く。", "マナ置きタップ")
        };

        public static IReadOnlyList<TemplateDef> All => _defs;

        public static IEnumerable<(TemplateKey key, Match match, bool ignore)> MatchAll(string sentence)
        {
            if (string.IsNullOrEmpty(sentence))
                yield break;

            foreach (var def in _defs)
            {
                Match m;
                try { m = def.Regex.Match(sentence); }
                catch { continue; }
                if (m.Success)
                    yield return (def.Key, m, def.Ignore);
            }
        }

        public static IReadOnlyList<(TemplateKey key, string pattern, bool ignore)> Snapshot()
            => _defs.Select(d => (d.Key, d.Regex.ToString(), d.Ignore)).ToList();
    }
}
