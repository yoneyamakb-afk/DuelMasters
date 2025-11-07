namespace DMRules.Engine.Tracing
{
    /// <summary>
    /// Test/Tools 向け：実トレースをスナップショット化するための最小IFとエントリポイント。
    /// 既存コードの変更は不要。必要な箇所から TraceSnapshot.Begin/Append/End を呼ぶだけ。
    /// 本番実行では Sink を設定しなければ何も起きません（no-op）。
    /// </summary>
    public interface ITraceSnapshotSink
    {
        void Begin(string name);
        void Append(object evt);
        void End();
    }

    public static class TraceSnapshot
    {
        public static ITraceSnapshotSink? Sink { get; set; }

        public static void Begin(string name) => Sink?.Begin(name);
        public static void Append(object evt) => Sink?.Append(evt);
        public static void End() => Sink?.End();
    }
}