using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace TraceDiff
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                var o = CliOptions.Parse(args);
                if (o.ShowHelp)
                {
                    Console.WriteLine(CliOptions.HelpText);
                    return 0;
                }

                if (!File.Exists(o.ExpectPath))
                {
                    Console.Error.WriteLine($"[TraceDiff] expect not found: {o.ExpectPath}");
                    return 2;
                }
                if (!File.Exists(o.ActualPath))
                {
                    Console.Error.WriteLine($"[TraceDiff] actual not found: {o.ActualPath}");
                    return 2;
                }

                var expected = await LoadJson(o.ExpectPath);
                var actual = await LoadJson(o.ActualPath);

                var diff = JsonDiff.Compare(expected, actual);
                var outDir = string.IsNullOrWhiteSpace(o.OutDir) ? Path.Combine("artifacts", "diff") : o.OutDir;
                Directory.CreateDirectory(outDir);

                var diffJsonPath = Path.Combine(outDir, "diff.json");
                var diffTxtPath  = Path.Combine(outDir, "diff.txt");

                await File.WriteAllTextAsync(diffJsonPath, JsonDiff.SafeClone(diff.Json).ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                await File.WriteAllTextAsync(diffTxtPath, diff.HumanReadable);

                Console.WriteLine($"[TraceDiff] wrote: {diffJsonPath}");
                Console.WriteLine($"[TraceDiff] wrote: {diffTxtPath}");
                Console.WriteLine($"[TraceDiff] equal: {diff.IsEqual}");
                return diff.IsEqual ? 0 : 3;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[TraceDiff] Fatal: " + ex);
                return 1;
            }
        }

        static async Task<JsonNode?> LoadJson(string path)
        {
            await using var fs = File.OpenRead(path);
            return await JsonNode.ParseAsync(fs);
        }
    }

    public record DiffResult(bool IsEqual, JsonObject Json, string HumanReadable);

    public static class JsonDiff
    {
        public static DiffResult Compare(JsonNode? a, JsonNode? b, string path = "$")
        {
            var result = new JsonObject { ["path"] = path };

            if (a == null && b == null)
                return new DiffResult(true, result, $"{path}: equal (both null)");

            if (a == null || b == null)
            {
                result["status"] = "mismatch";
                result["left"] = SafeClone(a);
                result["right"] = SafeClone(b);
                return new DiffResult(false, result, $"{path}: one side is null");
            }

            if (a is JsonValue && b is JsonValue)
            {
                var eq = a.ToJsonString() == b.ToJsonString();
                result["status"] = eq ? "equal" : "mismatch";
                if (!eq)
                {
                    result["left"] = SafeClone(a);
                    result["right"] = SafeClone(b);
                }
                return new DiffResult(eq, result, $"{path}: {(eq ? "equal" : "mismatch")}");
            }

            if (a is JsonArray arrA && b is JsonArray arrB)
            {
                bool allEq = arrA.Count == arrB.Count;
                var arrResult = new JsonArray();
                var lines = new System.Collections.Generic.List<string>();
                int n = Math.Max(arrA.Count, arrB.Count);
                for (int i = 0; i < n; i++)
                {
                    var ai = i < arrA.Count ? arrA[i] : null;
                    var bi = i < arrB.Count ? arrB[i] : null;
                    var sub = Compare(ai, bi, $"{path}[{i}]");
                    if (!sub.IsEqual) allEq = false;

                    // Always add a fresh clone to avoid parent conflicts
                    arrResult.Add(SafeClone(sub.Json));
                    lines.Add(sub.HumanReadable);
                }
                result["status"] = allEq ? "equal" : "mismatch";
                result["items"] = arrResult;
                return new DiffResult(allEq, result, string.Join(Environment.NewLine, lines));
            }

            if (a is JsonObject objA && b is JsonObject objB)
            {
                bool allEq = true;
                var objRes = new JsonObject();
                var lines = new System.Collections.Generic.List<string>();
                var keys = new System.Collections.Generic.SortedSet<string>();
                foreach (var kv in objA) keys.Add(kv.Key);
                foreach (var kv in objB) keys.Add(kv.Key);
                foreach (var k in keys)
                {
                    var sub = Compare(objA[k], objB[k], $"{path}.{k}");
                    if (!sub.IsEqual) allEq = false;

                    // Fresh clone for every insertion
                    objRes[k] = SafeClone(sub.Json);
                    lines.Add(sub.HumanReadable);
                }
                result["status"] = allEq ? "equal" : "mismatch";
                result["fields"] = objRes;
                return new DiffResult(allEq, result, string.Join(Environment.NewLine, lines));
            }

            // different kinds
            result["status"] = "mismatch";
            result["left"] = SafeClone(a);
            result["right"] = SafeClone(b);
            return new DiffResult(false, result, $"{path}: different kinds");
        }

        // Helper that guarantees a detached clone (never shares parent)
        public static JsonNode? SafeClone(JsonNode? node)
        {
            if (node is null) return null;
            // DeepClone() is available, but to be extra-safe across runtimes,
            // reparse from string to force a brand-new detached tree.
            return JsonNode.Parse(node.ToJsonString());
        }
    }

    public class CliOptions
    {
        public string ExpectPath { get; private set; } = "";
        public string ActualPath { get; private set; } = "";
        public string OutDir { get; private set; } = "";
        public bool ShowHelp { get; private set; } = false;

        public static string HelpText => @"TraceDiff
Usage:
  diff --expect <expected.json> --actual <actual.json> [--out <dir>]
";

        public static CliOptions Parse(string[] args)
        {
            var o = new CliOptions();
            if (args.Length == 0) { o.ShowHelp = true; return o; }
            if (args[0]?.Trim().ToLowerInvariant() != "diff") { o.ShowHelp = true; return o; }
            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--expect": o.ExpectPath = (i + 1 < args.Length) ? args[++i] : ""; break;
                    case "--actual": o.ActualPath = (i + 1 < args.Length) ? args[++i] : ""; break;
                    case "--out": o.OutDir = (i + 1 < args.Length) ? args[++i] : ""; break;
                    case "-h":
                    case "--help": o.ShowHelp = true; break;
                }
            }
            return o;
        }
    }
}
