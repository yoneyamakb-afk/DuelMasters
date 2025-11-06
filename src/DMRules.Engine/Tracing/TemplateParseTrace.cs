// M15.x - Template Parse Trace writer (unchanged)
using DMRules.Engine.Text.Overrides;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;


namespace DMRules.Engine.Tracing
{
    public sealed class TemplateParseTrace
    {
        public bool IncludeUnresolved { get; set; } = true;

        public sealed class Record
        {
            public string CardName { get; set; } = "";
            public List<string> Tokens { get; set; } = new();
            public List<string> Unresolved { get; set; } = new();
            public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        }

        private readonly List<Record> _records = new();

        public void Add(Record r) => _records.Add(r);

        public void WriteJson(string path)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };

            // M15.16s: Apply sanitization on a deep clone to avoid residual ShieldTrigger prefixes
            var sanitized = M15_16r_SnapshotSanitizer.RemoveShieldTriggerPrefix(_records).ToList();

            using var fs = File.Create(path);
            JsonSerializer.Serialize(fs, sanitized, options);
        }




    }
}
