
// src/DMRules.TraceExporter/M16/TraceEventExtensions.cs
// Ensure side/twin_id appear in the emitted trace if available.
using System.Collections.Generic;

namespace DMRules.TraceExporter.M16
{
    public static class TraceEventExtensions
    {
        public static IDictionary<string, object> WithTwinAndSide(this IDictionary<string, object> evt, string twinId, string side)
        {
            if (evt == null) return evt;
            if (!string.IsNullOrEmpty(twinId)) evt["twin_id"] = twinId;
            if (!string.IsNullOrEmpty(side)) evt["side"] = side;
            return evt;
        }
    }
}
