using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using DMRules.Engine.Tracing;
using DMRules.Tests.Snapshots; // SnapshotAssert

namespace DMRules.Tests.M19
{
    /// <summary>
    /// JSONバッファリングするテスト用Sink。
    /// End() 時に SnapshotAssert へ吐き出します。
    /// </summary>
    file sealed class JsonBufferTraceSink : ITraceSnapshotSink
    {
        private string _name = "trace_default";
        private readonly List<object> _events = new();

        public void Begin(string name) { _name = name; _events.Clear(); }
        public void Append(object evt) { _events.Add(evt); }
        public void End()
        {
            var payload = new { Name = _name, Events = _events };
            SnapshotAssert.MatchJson($"m19_trace_{_name}", payload);
        }
    }

    public class M19_TraceSnapshot_DemoTests
    {
        [Fact]
        public void Collects_And_Snapshots_DemoTrace()
        {
            // Arrange
            TraceSnapshot.Sink = new JsonBufferTraceSink();

            // Act: 任意の場所でトレースを積むイメージ
            TraceSnapshot.Begin("playtwin_path");
            TraceSnapshot.Append(new { Type="PlayTwin", SourceFaceId=10, Side="B" });
            TraceSnapshot.Append(new { Type="PlayCard", FaceId=11, Zone="BattleZone" });
            TraceSnapshot.End();

            // Assert は Sink 側で SnapshotAssert が実施（ここでは何もしない）
        }
    }
}