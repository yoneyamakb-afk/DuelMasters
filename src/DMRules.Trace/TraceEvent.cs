using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DMRules.Trace
{
    /// <summary>1イベント=1行(JSONL) で書き出すためのイベントスキーマ。</summary>
    public sealed class TraceEvent
    {
        [JsonPropertyName("ts")]
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

        [JsonPropertyName("phase")]
        public string? Phase { get; init; }

        [JsonPropertyName("action")]
        public string? Action { get; init; }

        [JsonPropertyName("player")]
        public string? Player { get; init; }

        [JsonPropertyName("card")]
        public string? Card { get; init; }

        [JsonPropertyName("stackSize")]
        public int? StackSize { get; init; }

        [JsonPropertyName("state")]
        public string? StateHash { get; init; }

        [JsonPropertyName("details")]
        public Dictionary<string, object?>? Details { get; init; }
    }
}
