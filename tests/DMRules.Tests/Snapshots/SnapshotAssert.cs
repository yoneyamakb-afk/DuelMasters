using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Xunit;

namespace DMRules.Tests.Snapshots
{
    /// <summary>
    /// Minimal JSON snapshot matcher.
    /// - Looks for baseline under tests/DMRules.Tests/__snapshots__/{name}.json
    /// - If env M19_REBASELINE=1 -> writes current -> baseline (rebaseline)
    /// - Otherwise compares string equality.
    /// </summary>
    public static class SnapshotAssert
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static void MatchJson(string name, object? value)
            => MatchJson(name, JsonSerializer.Serialize(value, JsonOpts));

        public static void MatchJson(string name, string json)
        {
            var baseDir = Path.Combine("tests", "DMRules.Tests", "__snapshots__");
            Directory.CreateDirectory(baseDir);
            var path = Path.Combine(baseDir, name + ".json");

            var rebaseline = Environment.GetEnvironmentVariable("M19_REBASELINE");
            if (!File.Exists(path) || rebaseline == "1")
            {
                File.WriteAllText(path, Normalize(json), Encoding.UTF8);
                Assert.True(true, $"Snapshot (re)written: {path}");
                return;
            }

            var expected = File.ReadAllText(path, Encoding.UTF8);
            Assert.Equal(Normalize(expected), Normalize(json));
        }

        private static string Normalize(string s)
            => s.Replace("\r\n", "\n").TrimEnd();
    }
}