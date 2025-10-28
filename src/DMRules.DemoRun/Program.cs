using DMRules.Engine;
using DMRules.Engine.Tracing;
using System.Reflection;


class Program
{
    static int Main(string[] args)
    {
        string? runner = null;
        string outDir = "artifacts";
        string fmt = "both";

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--runner": if (i + 1 < args.Length) runner = args[++i]; break;
                case "--outdir": if (i + 1 < args.Length) outDir = args[++i]; break;
                case "--fmt": if (i + 1 < args.Length) fmt = args[++i]; break;
            }
        }

        Directory.CreateDirectory(outDir);

        var s = new GameState().AddTrace("Init", runner is null ? "DemoRun start" : $"RealRun start ({runner})");

        if (!string.IsNullOrWhiteSpace(runner))
        {
            try
            {
                var lastDot = runner.LastIndexOf('.');
                if (lastDot <= 0 || lastDot == runner.Length - 1)
                    throw new ArgumentException($"Invalid --runner: {runner}");

                var typeName = runner.Substring(0, lastDot);
                var methodName = runner.Substring(lastDot + 1);

                var type = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType(typeName, throwOnError: false, ignoreCase: false))
                    .FirstOrDefault(t => t != null)
                    ?? Type.GetType(typeName, throwOnError: true)!;

                var candidates = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                                     .Where(m => string.Equals(m.Name, methodName, StringComparison.Ordinal))
                                     .ToArray();

                MethodInfo? m =
                    candidates.FirstOrDefault(mi =>
                    {
                        var p = mi.GetParameters();
                        return mi.ReturnType == typeof(GameState) &&
                               p.Length == 1 && p[0].ParameterType == typeof(GameState);
                    })
                    ?? candidates.FirstOrDefault(mi =>
                    {
                        var p = mi.GetParameters();
                        return mi.ReturnType == typeof(GameState) &&
                               p.Length == 2 && p[0].ParameterType == typeof(GameState) &&
                               p[1].ParameterType == typeof(string[]);
                    });

                if (m == null)
                    throw new MissingMethodException($"Method not found or wrong signature: {runner} (need GameState -> GameState)");

                s = (m.GetParameters().Length == 1)
                    ? (GameState)m.Invoke(null, new object[] { s })!
                    : (GameState)m.Invoke(null, new object[] { s, args })!;

                s = s.AddTrace("End", "RealRun end");
            }
            catch (Exception ex)
            {
                s = s.AddTrace("Runner.Error", ex.GetType().Name + ": " + ex.Message)
                     .AddTrace("Default", "Fallback to demo");
            }
        }
        else
        {
            s = s.AddTrace("Default", "DemoRun end");
        }

        var jsonPath = Path.Combine(outDir, "trace.json");
        var ndjsonPath = Path.Combine(outDir, "trace.ndjson");

        if (fmt == "json" || fmt == "both") TraceExporter.WriteJson(s.Trace, jsonPath);
        if (fmt == "ndjson" || fmt == "both") TraceExporter.WriteNdjson(s.Trace, ndjsonPath);

        Console.WriteLine("Trace written:");
        if (File.Exists(jsonPath)) Console.WriteLine("  " + jsonPath);
        if (File.Exists(ndjsonPath)) Console.WriteLine("  " + ndjsonPath);
        return 0;
    }
}
