
using DMRules.Effects.Bridge;
using DMRules.Effects.Text;

namespace DuelMasters.Engine.Integration
{
    /// <summary>
    /// M12.4: デリゲートを明示しなくても、登録済みの EffectsEngineActions を使って呼べるオーバーロード。
    /// エンジン側は起動時に EffectsEngineActions.DrawCards / AddMana を一度設定するだけでOK。
    /// </summary>
    internal static partial class EffectWiring
    {
        public static void OnEnterBattlezone(
            IEffectTextProvider provider,
            int faceId,
            int controllerId)
        {
            var text = provider.GetEffectTextByFaceId(faceId);
            EffectRunner.RunOnEnterBattlezone(
                text,
                EffectsEngineActions.DrawCards,
                EffectsEngineActions.AddMana,
                controllerId);
        }

        public static void OnAttackDeclared(
            IEffectTextProvider provider,
            int faceId,
            int controllerId)
        {
            var text = provider.GetEffectTextByFaceId(faceId);
            EffectRunner.RunOnAttackDeclared(
                text,
                EffectsEngineActions.DrawCards,
                EffectsEngineActions.AddMana,
                controllerId);
        }
    }
}
