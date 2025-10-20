using System;
using System.Runtime.CompilerServices;
using DMRules.Trace;

namespace DMRules.Tests
{
    // ModuleInitializerでテスト実行時に自動起動（xUnit型に依存しない）
    internal static class TraceBootstrap
    {
        [ModuleInitializer]
        public static void Init()
        {
            try
            {
                TraceExporter.Initialize(new TraceOptions { Enabled = null, OutputDir = null });
                TraceExporter.Write(new TraceEvent { Action = "test_session_start" });
                AppDomain.CurrentDomain.ProcessExit += (_, __) =>
                {
                    try
                    {
                        TraceExporter.Write(new TraceEvent { Action = "test_session_end" });
                        TraceExporter.Flush(TimeSpan.FromSeconds(2));
                        TraceExporter.Shutdown();
                    }
                    catch { }
                };
            }
            catch { }
        }
    }
}
