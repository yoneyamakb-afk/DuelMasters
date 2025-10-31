using System;
using System.IO;
using DMRules.Engine.Tracing;

namespace DMRules.ReplayTool
{
    internal static class Program
    {
        static int Main(string[] args)
        {
            // Usage: DMRules.ReplayTool --in <path-to-ndjson> [--outdir <dir>]
            string? inPath = null;
            string? outDir = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--in" && i + 1 < args.Length) inPath = args[++i];
                else if (args[i] == "--outdir" && i + 1 < args.Length) outDir = args[++i];
            }

            if (string.IsNullOrWhiteSpace(inPath) || !File.Exists(inPath))
            {
                Console.Error.WriteLine("ReplayTool error: --in <existing ndjson file> is required.");
                return 2;
            }

            // Ensure tracing enabled and directed
            Environment.SetEnvironmentVariable("DM_TRACE", "1");
            if (!string.IsNullOrWhiteSpace(outDir))
            {
                Directory.CreateDirectory(outDir);
                Environment.SetEnvironmentVariable("DM_TRACE_DIR", outDir);
            }

            TraceExporter.Initialize(new TraceOptions { OutputDir = outDir });

            // Mark start
            TraceExporter.Write(new TraceEvent { Action = "replay_session_start", Details = new() { { "source", inPath } } });

            int lines = 0;
            using (var sr = new StreamReader(inPath))
            {
                string? line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    // Pass through as-is: each NDJSON line becomes one trace line
                    TraceExporter.WriteNdjson(line, outDir);
                    lines++;
                }
            }

            // Mark end
            TraceExporter.Write(new TraceEvent { Action = "replay_session_end", Details = new() { { "lines", lines } } });
            TraceExporter.Shutdown();
            return 0;
        }
    }
}
