
using System;
using DMRules.Effects;

namespace DMRules.Effects.Bridge
{
    /// <summary>
    /// Engine非依存で使える効果実行ヘルパ。
    /// エンジン側の実処理（ドロー/マナ付与など）をデリゲートで注入して使います。
    /// </summary>
    public static class EffectRunner
    {
        /// <summary>召喚/バトルゾーンに出た時のトリガーを実行。</summary>
        public static void RunOnEnterBattlezone(
            string? effectText,
            Action<int,int> drawCards,
            Action<int,int> addMana,
            int controllerId)
        {
            var eff = EffectParser.Parse(effectText ?? string.Empty);
            var plan = EffectPlanner.Plan(eff);
            var host = new DelegateHost(drawCards, addMana);
            plan.OnEnterBattlezone(host, controllerId);
        }

        /// <summary>攻撃宣言のトリガーを実行。</summary>
        public static void RunOnAttackDeclared(
            string? effectText,
            Action<int,int> drawCards,
            Action<int,int> addMana,
            int controllerId)
        {
            var eff = EffectParser.Parse(effectText ?? string.Empty);
            var plan = EffectPlanner.Plan(eff);
            var host = new DelegateHost(drawCards, addMana);
            plan.OnAttackDeclared(host, controllerId);
        }

        private sealed class DelegateHost : IEffectHost
        {
            private readonly Action<int,int> _draw;
            private readonly Action<int,int> _mana;
            public DelegateHost(Action<int,int> draw, Action<int,int> mana)
            {
                _draw = draw; _mana = mana;
            }
            public void DrawCards(int playerId, int count) => _draw(playerId, count);
            public void AddMana(int playerId, int count) => _mana(playerId, count);
        }
    }
}
