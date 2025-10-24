
using System;
using DMRules.Effects.Bridge;

namespace DuelMasters.Engine.Integration
{
    /// <summary>
    /// M12.2: エンジンの呼び出し点に置く、最小配線ヘルパ。
    /// エンジン内部の既存処理をデリゲートで注入することで、IR/Planner 実行を安全に呼び出します。
    /// </summary>
    internal static class EffectWiring
    {
        /// <summary>召喚/バトルゾーンに出た直後に呼ぶ想定の入口。</summary>
        public static void OnEnterBattlezone(
            string? effectText,
            int controllerId,
            Action<int,int> drawCards,
            Action<int,int> addMana)
        {
            EffectRunner.RunOnEnterBattlezone(effectText, drawCards, addMana, controllerId);
        }

        /// <summary>攻撃宣言時に呼ぶ想定の入口。</summary>
        public static void OnAttackDeclared(
            string? effectText,
            int controllerId,
            Action<int,int> drawCards,
            Action<int,int> addMana)
        {
            EffectRunner.RunOnAttackDeclared(effectText, drawCards, addMana, controllerId);
        }
    }
}
