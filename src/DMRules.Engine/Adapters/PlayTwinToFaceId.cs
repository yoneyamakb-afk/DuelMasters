using DMRules.Engine.Events;
using DMRules.Engine.Services;

namespace DMRules.Engine.Adapters
{
    /// <summary>
    /// PlayTwin -> FaceId 解決の簡易ファサード。
    /// 既存のゲーム状態やPlayCard経路に影響しない範囲で、安全に導入できます。
    /// </summary>
    public static class PlayTwinToFaceId
    {
        public static int Resolve(PlayTwin e, ITwinFaceResolver resolver)
            => resolver.ResolveFaceId(e.SourceFaceId, e.SideToPlay);
    }
}