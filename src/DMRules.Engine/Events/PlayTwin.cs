namespace DMRules.Engine.Events
{
    /// <summary>
    /// TwinImpact の任意面（A/B）を指定してプレイするイベント（最小依存版）。
    /// </summary>
    public sealed record PlayTwin(int SourceFaceId, int SideToPlay /* 0=A,1=B */);
}