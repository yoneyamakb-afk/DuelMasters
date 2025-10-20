using System;
using System.Collections.Generic;

namespace DMRules.Data
{
    public sealed class CardRecord
    {
        public required IReadOnlyDictionary<string, object?> Data { get; init; }

        private string? GetString(string column)
            => Data.TryGetValue(column, out var v) && v is not null ? v.ToString() : null;

        private long? GetLong(string column)
        {
            if (!Data.TryGetValue(column, out var v) || v is null) return null;
            try
            {
                if (v is long l) return l;
                if (v is int i) return (long)i;
                if (long.TryParse(v.ToString(), out var parsed)) return parsed;
            }
            catch { }
            return null;
        }

        public string? CardName => GetString("cardname") ?? GetString("name");
        public string? CivilTxt => GetString("civiltxt");
        public string? TypeTxt  => GetString("typetxt");
        public string? PowerTxt => GetString("powertxt");
        public string? CostTxt  => GetString("costtxt");
        public long? FaceId     => GetLong("face_id") ?? GetLong("id");
    }
}
