
using System;
using System.Text;

namespace DMRules.Tests.Snapshots
{
    public static class SnapshotDiffUtils
    {
        public static string Unified(string expected, string actual)
        {
            var a = expected.Replace("\r\n","\n").Split('\n');
            var b = actual.Replace("\r\n","\n").Split('\n');
            var sb = new StringBuilder();
            int max = Math.Max(a.Length, b.Length);
            for (int i=0; i<max; i++)
            {
                var ea = i < a.Length ? a[i] : "";
                var eb = i < b.Length ? b[i] : "";
                if (!string.Equals(ea, eb, StringComparison.Ordinal))
                {
                    sb.AppendLine($"@@ line {i+1} @@");
                    sb.AppendLine($"- {ea}");
                    sb.AppendLine($"+ {eb}");
                }
            }
            return sb.ToString();
        }
    }
}
