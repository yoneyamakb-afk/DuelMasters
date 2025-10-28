using System;
using System.IO;
using DMRules.Engine.Tracing;
using Xunit;

namespace DMRules.Tests
{
    public class TraceSmokeTest
    {
        [Fact(DisplayName = "TraceExporter writes JSONL when DM_TRACE=1")]
        public void WritesJsonlWhenEnabled()
        {
            // DM_TRACEが未設定の環境では何もしない（成功扱い）
            var enabled = Environment.GetEnvironmentVariable("DM_TRACE");
            if (string.IsNullOrWhiteSpace(enabled)) return;

            var dir = Environment.GetEnvironmentVariable("DM_TRACE_DIR");
            dir ??= Path.Combine(Environment.CurrentDirectory, ".trace");

            TraceExporter.Write(new TraceEvent { Action = "from_smoke_test" });
            TraceExporter.Flush(TimeSpan.FromSeconds(2));

            Assert.True(Directory.Exists(dir), $"Trace dir not found: {dir}");
            var files = Directory.GetFiles(dir, "duel_*.jsonl");
            Assert.NotEmpty(files);
            var size = new FileInfo(files[^1]).Length;
            Assert.True(size > 0, "Trace file is empty");
        }
    }
}
