using System;
using System.IO;
using DMRules.Engine.TextParsing;
using Microsoft.Data.Sqlite;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CsvHelper;

namespace DMRules.Tools
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            if (args.Length > 0 && args[0].Equals("delta", StringComparison.OrdinalIgnoreCase))
            {
                // Shift args and call delta
                var shifted = args.Skip(1).ToArray();
                return DeltaProgram.Run(shifted);
            }
            // Fallback: quick info
            Console.Error.WriteLine("This build contains the DELTA tool.\nUse:\n  dotnet run --project src/DMRules.Tools -- delta <old.json> <new.json> [--out ./artifacts]");
            return 2;
        }
    }
}
