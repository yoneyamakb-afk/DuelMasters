
using System;
using DMRules.Effects.Bridge;
using DMRules.Effects.Text;

namespace DuelMasters.Engine.Integration
{
    /// <summary>
    /// M12.3: テキストプロバイダを直接受け取るオーバーロードを追加。
    /// エンジン側は faceId とデリゲートだけ渡せばよい。
    /// </summary>
    internal static partial class EffectWiring // partial として拡張
    {
        public static void OnEnterBattlezone(
            IEffectTextProvider provider,
            int faceId,
            int controllerId,
            Action<int,int> drawCards,
            Action<int,int> addMana)
        {
            var text = provider.GetEffectTextByFaceId(faceId);
            EffectRunner.RunOnEnterBattlezone(text, drawCards, addMana, controllerId);
        }

        public static void OnAttackDeclared(
            IEffectTextProvider provider,
            int faceId,
            int controllerId,
            Action<int,int> drawCards,
            Action<int,int> addMana)
        {
            var text = provider.GetEffectTextByFaceId(faceId);
            EffectRunner.RunOnAttackDeclared(text, drawCards, addMana, controllerId);
        }
    }
}
