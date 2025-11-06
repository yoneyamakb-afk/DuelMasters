using DMRules.Engine.Events;
using DMRules.Engine.Services;

namespace DMRules.Engine.Handlers
{
    public static class PlayTwinHandler
    {
        /// <summary>
        /// Twin面を解決して FaceId を返す（TraceExporter への依存を除去）。
        /// </summary>
        public static int Handle(PlayTwin e, ITwinFaceResolver resolver)
        {
            var targetFaceId = resolver.ResolveFaceId(e.SourceFaceId, e.SideToPlay);
            return targetFaceId;
        }
    }
}