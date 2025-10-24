
using System;

namespace DuelMasters.Engine.Integration
{
    /// <summary>
    /// M12.4: エンジン側の実行関数を登録するためのレジストリ。
    /// 既存のドロー／マナ加算の関数をここにセットすると、効果解決から呼ばれるようになります。
    /// 何も設定しない場合は No-Op。ビルドは常に安定します。
    /// </summary>
    public static class EffectsEngineActions
    {
        /// <summary>実際のドロー処理（playerId, count）</summary>
        public static Action<int,int> DrawCards { get; set; } = static (_, __) => { };

        /// <summary>実際のマナ加算処理（playerId, count）</summary>
        public static Action<int,int> AddMana  { get; set; } = static (_, __) => { };
    }
}
