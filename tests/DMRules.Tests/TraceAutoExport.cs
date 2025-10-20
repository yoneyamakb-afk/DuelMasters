using System.Collections.Generic;
using System.IO;
using Xunit;
using DMRules.Engine;

namespace DMRules.Tests
{
    public class TraceAutoExport
    {
        [Fact(DisplayName = "Auto-export demo trace to artifacts/trace.(json|ndjson)")]
        public void Export_Demo_Trace_For_Report()
        {
            var trace = new List<TraceEntry>
            {
                new(0, "Init", "Demo start"),
                new(1, "Repl.Candidates", "2 match for Destroy"),
                new(2, "Repl.Choose", "[P0] prio=10 PreventNextDestroy"),
                new(3, "Repl.Apply", "PreventNextDestroy applied"),
                new(4, "Default", "Demo end")
            };

            Directory.CreateDirectory("artifacts");
            TraceExporter.WriteJson(trace, "artifacts/trace.json");
            TraceExporter.WriteNdjson(trace, "artifacts/trace.ndjson");
        }
    }
}
