// src/DMRules.TraceReport/M16/EventShape.cs
// ASCII-only. Minimal model + parser helpers for generic traces.
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace DMRules.TraceReport.M16
{
    internal sealed class EventShape
    {
        public string Phase { get; init; } = "";
        public string Action { get; init; } = "";
        public string Actor  { get; init; } = "";
        public string Side   { get; init; } = "";
        public string TwinId { get; init; } = "";
        public string CardName { get; init; } = "";
        public string Note   { get; init; } = "";

        public static string S(JsonElement e, string key)
        {
            if (e.ValueKind != JsonValueKind.Object) return "";
            if (e.TryGetProperty(key, out var v))
            {
                if (v.ValueKind == JsonValueKind.String) return v.GetString() ?? "";
                if (v.ValueKind == JsonValueKind.Number) return v.ToString();
            }
            return "";
        }

        public static EventShape From(JsonElement e)
        {
            string cardSide = "";
            string cardName = "";
            string twinId   = "";

            if (e.ValueKind == JsonValueKind.Object)
            {
                if (e.TryGetProperty("card", out var card) && card.ValueKind == JsonValueKind.Object)
                {
                    cardSide = S(card, "side");
                    cardName = S(card, "name");
                    twinId   = S(card, "twin_id");
                }
                if (string.IsNullOrEmpty(twinId)) twinId = S(e, "twin_id");
                if (string.IsNullOrEmpty(cardSide)) cardSide = S(e, "side");
            }

            string note = "";
            if (e.TryGetProperty("note", out var noteObj))
            {
                if (noteObj.ValueKind == JsonValueKind.Object)
                {
                    // flatten one level of note
                    foreach (var p in noteObj.EnumerateObject())
                    {
                        note = (p.Value.ValueKind == JsonValueKind.String) ? (p.Value.GetString() ?? "") : p.Value.ToString();
                        break;
                    }
                }
                else if (noteObj.ValueKind == JsonValueKind.String)
                {
                    note = noteObj.GetString() ?? "";
                }
            }

            return new EventShape
            {
                Phase = S(e, "phase"),
                Action = S(e, "action"),
                Actor = S(e, "actor"),
                Side = cardSide,
                TwinId = twinId,
                CardName = cardName,
                Note = note
            };
        }
    }
}
