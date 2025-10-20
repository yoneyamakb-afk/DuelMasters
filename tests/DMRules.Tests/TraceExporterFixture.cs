using System;
using System.IO;
using DMRules.Trace;
using Xunit;

namespace DMRules.Tests
{
    /// <summary>
    /// Initializes TraceExporter for the whole test assembly when DM_TRACE is enabled.
    /// Usage: setx DM_TRACE 1  (restart shell)  and optional setx DM_TRACE_DIR C:\path\to\traces
    /// </summary>
    public sealed class TraceExporterFixture : IDisposable
    {
        public TraceExporterFixture()
        {
            TraceExporter.Initialize(new TraceOptions
            {
                Enabled = null, // respect env DM_TRACE
                OutputDir = null
            });

            // Example: write a banner event so it's clear when a test session begins
            TraceExporter.Write(new TraceEvent
            {
                Action = "test_session_start",
                Details = new() { { "framework", "xunit" }, { "assembly", typeof(TraceExporterFixture).Assembly.FullName! } }
            });
        }

        public void Dispose()
        {
            TraceExporter.Write(new TraceEvent { Action = "test_session_end" });
            TraceExporter.Flush(TimeSpan.FromSeconds(2));
            TraceExporter.Shutdown();
        }
    }

    [CollectionDefinition("TraceExporter-collection")]
    public class TraceExporterCollection : ICollectionFixture<TraceExporterFixture> { }
}
