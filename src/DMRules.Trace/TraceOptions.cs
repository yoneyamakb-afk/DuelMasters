namespace DMRules.Trace
{
    public sealed class TraceOptions
    {
        /// <summary>有効/無効。null なら環境変数 DM_TRACE を見る(1/true/on)。</summary>
        public bool? Enabled { get; init; }

        /// <summary>出力ディレクトリ。null なら DM_TRACE_DIR または .\.trace。</summary>
        public string? OutputDir { get; init; }

        /// <summary>ファイル名プレフィックス。既定 duel。</summary>
        public string FilePrefix { get; init; } = "duel";

        /// <summary>追記モード（既定 true）。</summary>
        public bool Append { get; init; } = true;
    }
}
