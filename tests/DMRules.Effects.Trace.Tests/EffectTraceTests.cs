
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DMRules.Effects;
using DMRules.Effects.Trace;
using Xunit;

public class EffectTraceTests
{
    [Fact]
    public void JsonlSink_WritesLines()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".jsonl");
        try
        {
            using var sink = new JsonlFileSink(path, append:false);
            EffectTrace.SetSink(sink);

            var eff = EffectParser.Parse("on summon: draw 2");
            var actions = IrTraceMapper.ExtractActions(eff, EffectIR.Trigger.EnterBattlezone);
            EffectTrace.Emit(new EffectTraceEvent(
                Timestamp: DateTimeOffset.UtcNow,
                Phase: EffectPhase.BeforeExecute,
                Trigger: EffectIR.Trigger.EnterBattlezone.ToString(),
                ControllerId: 0,
                FaceId: 1,
                CardName: "Dummy",
                RawText: "on summon: draw 2",
                Actions: actions
            ));

            sink.Dispose();
            var lines = File.ReadAllLines(path);
            Assert.True(lines.Length >= 1);
            var doc = JsonDocument.Parse(lines[^1]);
            Assert.Equal("BeforeExecute", doc.RootElement.GetProperty("Phase").GetString());
        }
        finally
        {
            EffectTrace.Reset();
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
