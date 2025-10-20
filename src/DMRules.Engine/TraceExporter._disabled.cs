using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace DMRules.Engine
{
    public static class TraceExporter
    {
        public static void WriteJson(IEnumerable<TraceEntry> trace, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            var json = JsonSerializer.Serialize(trace, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        public static void WriteNdjson(IEnumerable<TraceEntry> trace, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            var sb = new StringBuilder();
            foreach (var t in trace) sb.AppendLine(JsonSerializer.Serialize(t));
            File.WriteAllText(path, sb.ToString());
        }
    }
}
