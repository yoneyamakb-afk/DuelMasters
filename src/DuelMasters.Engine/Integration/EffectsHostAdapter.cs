
using DMRules.Effects;

namespace DuelMasters.Engine.Integration
{
    /// <summary>
    /// M12.4: IEffectHost の本実装。
    /// 中で EffectsEngineActions に登録された実エンジン処理を呼び出します。
    /// </summary>
    internal sealed class EffectsHostAdapter : IEffectHost
    {
        public void DrawCards(int playerId, int count)
            => EffectsEngineActions.DrawCards(playerId, count);

        public void AddMana(int playerId, int count)
            => EffectsEngineActions.AddMana(playerId, count);
    }
}
