
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

    public class M21_APNAP_And_Chains_Tests
    {
        [Fact]
        public void Twin_APNAP_Order()
        {
            TraceSnapshot.Sink = new JsonBufferTraceSink();
            var resolver = new InMemoryTwinResolver(new() { {(600,1), 601}, {(700,0), 699} });
            var play = new SpyPlayCard();
            var uc = new PlayTwinIntoPlayCard(resolver, play);

            TraceSnapshot.Begin("apnap_order");
            TraceSnapshot.Append(new { Type="Setup", Active="P1", NonActive="P2" });

            // P1(ACTIVE) chooses B-side for twin (600->601)
            uc.Execute(new PlayTwin(600, 1));
            TraceSnapshot.Append(new { Type="APNAP_Active_Resolved", FaceIds=play.Played.ToArray() });

            // P2(NON-ACTIVE) chooses A-side for twin (700->699)
            uc.Execute(new PlayTwin(700, 0));
            TraceSnapshot.Append(new { Type="APNAP_NonActive_Resolved", FaceIds=play.Played.ToArray() });

            TraceSnapshot.End();
        }

        [Fact]
        public void Twin_Chain_Destroy()
        {
            TraceSnapshot.Sink = new JsonBufferTraceSink();
            var resolver = new InMemoryTwinResolver(new() { {(800,1), 801}, {(801,0), 800} });
            var play = new SpyPlayCard();
            var uc = new PlayTwinIntoPlayCard(resolver, play);

            TraceSnapshot.Begin("chain_destroy");
            TraceSnapshot.Append(new { Type="Setup", TwinId=314, A=800, B=801 });

            // A enters (simulate)
            play.PlayByFaceId(800);
            TraceSnapshot.Append(new { Type="Enter", FaceId=800 });

            // Destroy A
            TraceSnapshot.Append(new { Type="Destroy", FaceId=800, To="Graveyard" });

            // Play B via Twin
            uc.Execute(new PlayTwin(800, 1));
            TraceSnapshot.Append(new { Type="After_B", FaceIds=play.Played.ToArray() });

            // Destroy B
            TraceSnapshot.Append(new { Type="Destroy", FaceId=801, To="Graveyard" });

            // Back to A via Twin
            uc.Execute(new PlayTwin(801, 0));
            TraceSnapshot.Append(new { Type="After_A2", FaceIds=play.Played.ToArray() });

            TraceSnapshot.End();
        }

        [Fact]
        public void Twin_ShieldTrigger_Mix()
        {
            TraceSnapshot.Sink = new JsonBufferTraceSink();
            var resolver = new InMemoryTwinResolver(new() { {(900,1), 901} });
            var play = new SpyPlayCard();
            var uc = new PlayTwinIntoPlayCard(resolver, play);

            TraceSnapshot.Begin("mix_trigger");
            TraceSnapshot.Append(new { Type="Setup", TwinId=271, A=900, B=901 });

            // Break shields -> stack: Trigger (P2) then Twin (P1) resolved APNAP
            TraceSnapshot.Append(new { Type="BreakShields", Count=1 });

            // Active player declares Twin first, but APNAP resolves NonActive's trigger first
            TraceSnapshot.Append(new { Type="Stack_Push", Who="P1", What="Twin(B)" });
            TraceSnapshot.Append(new { Type="Stack_Push", Who="P2", What="ShieldTrigger(Cast 9100)" });

            // Resolve NonActive (P2) first
            TraceSnapshot.Append(new { Type="Resolve", Who="P2", What="ShieldTrigger", CardFaceId=9100, Cast=true });

            // Now resolve Active (P1) twin play
            uc.Execute(new PlayTwin(900, 1));
            TraceSnapshot.Append(new { Type="Resolve", Who="P1", What="TwinPlay", FaceIds=play.Played.ToArray() });

            TraceSnapshot.End();
        }
    }
}
