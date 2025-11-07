
using System.Collections.Generic;
using Xunit;
using DMRules.Engine.Tracing;
using DMRules.Tests.Snapshots;
using DMRules.Engine.Services;
using DMRules.Engine.UseCases;
using DMRules.Engine.Interfaces;
using DMRules.Engine.Events;

namespace DMRules.Tests.M21
{
    file sealed class JsonBufferTraceSink : ITraceSnapshotSink
    {
        private string _name = "trace_default";
        private readonly List<object> _events = new();
        public void Begin(string name) { _name = name; _events.Clear(); }
        public void Append(object evt) { _events.Add(evt); }
        public void End()
        {
            var payload = new { Name = _name, Events = _events };
            SnapshotAssert.MatchJson($"m21_{_name}", payload);
        }
    }

    file sealed class InMemoryTwinResolver : ITwinFaceResolver
    {
        private readonly Dictionary<(int source, int side), int> _map;
        public InMemoryTwinResolver(Dictionary<(int,int), int> map) => _map = map;
        public int ResolveFaceId(int sourceFaceId, int sideToPlay) => _map[(sourceFaceId, sideToPlay)];
    }

    file sealed class SpyPlayCard : IPlayCardByFaceId
    {
        public List<int> Played = new();
        public void PlayByFaceId(int faceId) { Played.Add(faceId); }
    }

    public class M21_TwinImpact_SnapshotTests
    {
        [Fact]
        public void A_to_B_play_path()
        {
            TraceSnapshot.Sink = new JsonBufferTraceSink();
            var resolver = new InMemoryTwinResolver(new() { {(100,1), 101} });
            var play = new SpyPlayCard();
            var uc = new PlayTwinIntoPlayCard(resolver, play);

            TraceSnapshot.Begin("twin_AtoB");
            TraceSnapshot.Append(new { Type="Setup", TwinId=55, A=100, B=101 });
            uc.Execute(new PlayTwin(100, 1));
            TraceSnapshot.Append(new { Type="PlayCardCalled", FaceIds=play.Played.ToArray() });
            TraceSnapshot.End();
        }

        [Fact]
        public void B_to_A_play_path()
        {
            TraceSnapshot.Sink = new JsonBufferTraceSink();
            var resolver = new InMemoryTwinResolver(new() { {(201,0), 200} });
            var play = new SpyPlayCard();
            var uc = new PlayTwinIntoPlayCard(resolver, play);

            TraceSnapshot.Begin("twin_BtoA");
            TraceSnapshot.Append(new { Type="Setup", TwinId=77, A=200, B=201 });
            uc.Execute(new PlayTwin(201, 0));
            TraceSnapshot.Append(new { Type="PlayCardCalled", FaceIds=play.Played.ToArray() });
            TraceSnapshot.End();
        }

        [Fact]
        public void A_play_then_destroy_then_B_play_same_turn()
        {
            TraceSnapshot.Sink = new JsonBufferTraceSink();
            var resolver = new InMemoryTwinResolver(new() { {(310,1), 311} });
            var play = new SpyPlayCard();
            var uc = new PlayTwinIntoPlayCard(resolver, play);

            TraceSnapshot.Begin("twin_A_destroy_B");
            TraceSnapshot.Append(new { Type="Setup", TwinId=88, A=310, B=311 });
            play.PlayByFaceId(310);
            TraceSnapshot.Append(new { Type="Destroy", FaceId=310, To="Graveyard" });
            uc.Execute(new PlayTwin(310, 1));
            TraceSnapshot.Append(new { Type="PlayCardCalled", FaceIds=play.Played.ToArray() });
            TraceSnapshot.End();
        }

        [Fact]
        public void shield_trigger_parallel_with_twin_play()
        {
            TraceSnapshot.Sink = new JsonBufferTraceSink();
            var resolver = new InMemoryTwinResolver(new() { {(420,1), 421} });
            var play = new SpyPlayCard();
            var uc = new PlayTwinIntoPlayCard(resolver, play);

            TraceSnapshot.Begin("twin_trigger_shield");
            TraceSnapshot.Append(new { Type="Setup", TwinId=99, A=420, B=421 });
            TraceSnapshot.Append(new { Type="BreakShields", Count=1 });
            TraceSnapshot.Append(new { Type="ShieldTrigger", CardFaceId=9001, Cast=true });
            uc.Execute(new PlayTwin(420, 1));
            TraceSnapshot.Append(new { Type="PlayCardCalled", FaceIds=play.Played.ToArray() });
            TraceSnapshot.End();
        }
    }
}
