// src/DMRules.TraceReport/M16/ParserExtensions.cs
// ASCII-only. Utility methods that a host program can call via reflection or direct reference.
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace DMRules.TraceReport.M16
{
    internal static class ParserExtensions
    {
        // Try to parse a trace JSON element into EventShape; returns false on failure.
        internal static bool TryParseEvent(JsonElement elem, out EventShape shape)
        {
            try
            {
                shape = EventShape.From(elem);
                return true;
            }
            catch
            {
                shape = null!;
                return false;
            }
        }

        // Try to describe an element into Kind/Detail using Describe.
        internal static bool TryDescribe(JsonElement elem, out string kind, out string detail)
        {
            kind = ""; detail = "";
            if (!TryParseEvent(elem, out var ev)) return false;
            var d = MarkdownFormatterCompat.Describe(ev);
            kind = d.Kind; detail = d.Detail;
            return true;
        }
    }
}
