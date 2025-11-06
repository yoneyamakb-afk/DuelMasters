// src/DMRules.TraceReport/M16/MarkdownFormatterCompat.cs
// ASCII-only. If the host TraceReport has any extensibility (partial/virtual), this can be wired.
// Otherwise, it is harmless and can be ignored by the host DLL.
using System;
using System.Collections.Generic;
using System.Text;

namespace DMRules.TraceReport.M16
{
    internal static class MarkdownFormatterCompat
    {
        // Convert an EventShape into Kind/Detail strings.
        internal static (string Kind, string Detail) Describe(EventShape ev)
        {
            if (ev is null) return ("Unknown", "");
            string kind = "";
            if (!string.IsNullOrEmpty(ev.Phase))  kind = ev.Phase;
            if (!string.IsNullOrEmpty(ev.Action)) kind = string.IsNullOrEmpty(kind) ? ev.Action : (kind + "/" + ev.Action);
            if (string.IsNullOrEmpty(kind)) kind = "Event";

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(ev.Actor)) sb.Append("Actor=").Append(ev.Actor).Append(' ');
            if (!string.IsNullOrEmpty(ev.CardName)) sb.Append("Card=").Append(ev.CardName).Append(' ');
            if (!string.IsNullOrEmpty(ev.Side)) sb.Append("Side=").Append(ev.Side).Append(' ');
            if (!string.IsNullOrEmpty(ev.TwinId)) sb.Append("TwinId=").Append(ev.TwinId).Append(' ');
            if (!string.IsNullOrEmpty(ev.Note)) sb.Append("Note=").Append(ev.Note).Append(' ');
            return (kind.Trim(), sb.ToString().Trim());
        }
    }
}
