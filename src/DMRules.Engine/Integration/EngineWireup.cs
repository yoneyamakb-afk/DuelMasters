using System;
using DMRules.Engine.Integration;
using DMRules.Engine.Tracing;

namespace DMRules.Engine.Integration
{
    /// <summary>
    /// ランタイム初期化（トレース配線など）を一箇所に集約。
    /// アプリ起動時／テスト初期化時に EngineWireup.Configure() を一度呼べばOK。
    /// </summary>
    public static class EngineWireup
    {
        private static bool _configured;

        public static void Configure(string? traceOutputDir = null)
        {
            if (_configured) return;
            _configured = true;

            // Trace 配線（必要に応じて強化）
            EngineHooks.OnTrace += msg => TraceExporter.Write(msg, traceOutputDir);

            // 例: SBA 前後で軽いログを吐く（必要に応じて削除/調整）
            EngineHooks.OnBeforeSBA += _ => EngineHooks.Trace("[Engine] Before SBA");
            EngineHooks.OnAfterSBA  += _ => EngineHooks.Trace("[Engine] After SBA");
        }
    }
}

// 後方互換: DuelMasters.Engine.Integration.EngineWireup
namespace DuelMasters.Engine.Integration
{
    public static class EngineWireup
    {
        public static void Configure(string? traceOutputDir = null)
            => DMRules.Engine.Integration.EngineWireup.Configure(traceOutputDir);
    }
}
