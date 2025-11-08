using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DMRules.Tests
{
    public class ReplayGoldenTests
    {
        [Fact]
        public void Duel_20251108_trace_should_match_replay_output()
        {
            var baseDir = AppContext.BaseDirectory;
            var solutionRoot = Path.GetFullPath(Path.Combine(
                baseDir, "..", "..", "..", "..", ".."));

            var inputPath = Path.Combine(
                solutionRoot,
                "tests", "DMRules.Tests", "ReplayInputs",
                "duel_20251108_084159.jsonl");

            var outputDir = Path.Combine(
                solutionRoot,
                "tests", "DMRules.Tests", "ReplayOutputs", ".trace");

            Assert.True(File.Exists(inputPath),
                $"Golden trace not found: {inputPath}");
            Assert.True(Directory.Exists(outputDir),
                $"Replay output directory not found: {outputDir}");

            var latestOutput = Directory
                .GetFiles(outputDir, "duel_*.jsonl")
                .OrderBy(p => p)
                .LastOrDefault();

            Assert.False(string.IsNullOrEmpty(latestOutput),
                $"No replay output duel_*.jsonl found in {outputDir}");

            var goldenLines = File.ReadAllLines(inputPath)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Trim())
                .ToArray();

            var actualLines = File.ReadAllLines(latestOutput)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Trim())
                // ✅ replay_session_start / end を除外
                .Where(l => !l.Contains("replay_session_start") && !l.Contains("replay_session_end"))
                .ToArray();

            Assert.Equal(goldenLines.Length, actualLines.Length);

            for (int i = 0; i < goldenLines.Length; i++)
            {
                Assert.Equal(goldenLines[i], actualLines[i]);
            }
        }
    }
}
