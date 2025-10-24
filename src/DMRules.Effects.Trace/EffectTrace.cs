
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using DMRules.Effects;

namespace DMRules.Effects.Trace
{
    public enum EffectPhase { BeforeExecute, AfterExecute }

    public sealed record EffectTraceAction(
        string Kind,
        int? IntParam // e.g., cards count
    );

    public sealed record EffectTraceEvent(
        DateTimeOffset Timestamp,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] EffectPhase Phase,
        string Trigger,
        int ControllerId,
        int? FaceId,
        string? CardName,
        string? RawText,
        IReadOnlyList<EffectTraceAction> Actions
    );

    public interface IEffectTraceSink : IDisposable
    {
        void Write(EffectTraceEvent e);
    }

    public sealed class NullSink : IEffectTraceSink
    {
        public static readonly NullSink Instance = new();
        private NullSink() {}
        public void Dispose() {}
        public void Write(EffectTraceEvent e) {}
    }

    /// <summary>Append-only JSON Lines (.jsonl) sink.</summary>
    public sealed class JsonlFileSink : IEffectTraceSink
    {
        private readonly object _gate = new();
        private readonly System.IO.StreamWriter _writer;

        public JsonlFileSink(string path, bool append = true)
        {
            var dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir!);
            _writer = new System.IO.StreamWriter(path, append: append, System.Text.Encoding.UTF8);
            _writer.AutoFlush = true;
        }

        public void Write(EffectTraceEvent e)
        {
            lock (_gate)
            {
                var json = JsonSerializer.Serialize(e, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                _writer.WriteLine(json);
            }
        }

        public void Dispose() => _writer.Dispose();
    }

    /// <summary>Global entry point to emit traces.</summary>
    public static class EffectTrace
    {
        private static IEffectTraceSink _sink = NullSink.Instance;

        public static void SetSink(IEffectTraceSink sink)
        {
            _sink = sink ?? NullSink.Instance;
        }

        public static void EnableJsonl(string path, bool append = true)
            => SetSink(new JsonlFileSink(path, append));

        public static void Reset() => SetSink(NullSink.Instance);

        public static void Emit(EffectTraceEvent e) => _sink.Write(e);
    }
}
