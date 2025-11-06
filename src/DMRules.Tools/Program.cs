
using System;

namespace DMRules.Tools
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("delta", StringComparison.OrdinalIgnoreCase))
                return DeltaProgram.Run(args[1..]);

            if (args.Length > 0 && args[0].Equals("scan", StringComparison.OrdinalIgnoreCase))
                return ScanProgram.Run(args[1..]);

            Console.Error.WriteLine(
@"Usage:
  dotnet run --project src/DMRules.Tools -- scan  --db <path-to-Duelmasters.db> [--limit 100] [--out ./artifacts]
  dotnet run --project src/DMRules.Tools -- delta <old.json> <new.json> [--out ./artifacts]");
            return 2;
        }
    }
}
