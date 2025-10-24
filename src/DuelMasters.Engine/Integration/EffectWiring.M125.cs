
using System;
using DMRules.Effects;
using DMRules.Effects.Bridge;
using DMRules.Effects.Text;
using DMRules.Effects.Trace;

namespace DuelMasters.Engine.Integration
{
    /// <summary>
    /// M12.5: トレース付きの実行ラッパー。
    /// 既存の OnEnterBattlezone/OnAttackDeclared と同等の挙動に、Before/After の JSONL トレースを追加します。
    /// </summary>
    internal static partial class EffectWiring
    {
        public static void OnEnterBattlezoneTraced(
            IEffectTextProvider provider,
            int faceId,
            int controllerId,
            string? cardName = null)
        {
            var text = provider.GetEffectTextByFaceId(faceId);
            var eff = EffectParser.Parse(text ?? string.Empty);
            var actions = IrTraceMapper.ExtractActions(eff, EffectIR.Trigger.EnterBattlezone);

            EffectTrace.Emit(new EffectTraceEvent(
                Timestamp: DateTimeOffset.UtcNow,
                Phase: EffectPhase.BeforeExecute,
                Trigger: nameof(EffectIR.Trigger.EnterBattlezone),
                ControllerId: controllerId,
                FaceId: faceId,
                CardName: cardName,
                RawText: text,
                Actions: actions
            ));

            // 実処理
            EffectRunner.RunOnEnterBattlezone(text,
                EffectsEngineActions.DrawCards,
                EffectsEngineActions.AddMana,
                controllerId);

            EffectTrace.Emit(new EffectTraceEvent(
                Timestamp: DateTimeOffset.UtcNow,
                Phase: EffectPhase.AfterExecute,
                Trigger: nameof(EffectIR.Trigger.EnterBattlezone),
                ControllerId: controllerId,
                FaceId: faceId,
                CardName: cardName,
                RawText: text,
                Actions: actions
            ));
        }

        public static void OnAttackDeclaredTraced(
            IEffectTextProvider provider,
            int faceId,
            int controllerId,
            string? cardName = null)
        {
            var text = provider.GetEffectTextByFaceId(faceId);
            var eff = EffectParser.Parse(text ?? string.Empty);
            var actions = IrTraceMapper.ExtractActions(eff, EffectIR.Trigger.AttackDeclared);

            EffectTrace.Emit(new EffectTraceEvent(
                Timestamp: DateTimeOffset.UtcNow,
                Phase: EffectPhase.BeforeExecute,
                Trigger: nameof(EffectIR.Trigger.AttackDeclared),
                ControllerId: controllerId,
                FaceId: faceId,
                CardName: cardName,
                RawText: text,
                Actions: actions
            ));

            // 実処理
            EffectRunner.RunOnAttackDeclared(text,
                EffectsEngineActions.DrawCards,
                EffectsEngineActions.AddMana,
                controllerId);

            EffectTrace.Emit(new EffectTraceEvent(
                Timestamp: DateTimeOffset.UtcNow,
                Phase: EffectPhase.AfterExecute,
                Trigger: nameof(EffectIR.Trigger.AttackDeclared),
                ControllerId: controllerId,
                FaceId: faceId,
                CardName: cardName,
                RawText: text,
                Actions: actions
            ));
        }
    }
}
