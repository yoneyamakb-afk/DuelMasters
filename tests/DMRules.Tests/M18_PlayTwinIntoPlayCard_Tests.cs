
using System.Collections.Generic;
using Xunit;
using DMRules.Engine.Events;
using DMRules.Engine.Services;
using DMRules.Engine.Interfaces;
using DMRules.Engine.UseCases;

namespace DMRules.Tests.M18
{
    file sealed class InMemoryTwinResolver : ITwinFaceResolver
    {
        private readonly Dictionary<(int source, int side), int> _map;
        public InMemoryTwinResolver(Dictionary<(int,int), int> map) => _map = map;
        public int ResolveFaceId(int sourceFaceId, int sideToPlay) => _map[(sourceFaceId, sideToPlay)];
    }

    file sealed class SpyPlayCard : IPlayCardByFaceId
    {
        public int? LastFaceId { get; private set; }
        public void PlayByFaceId(int faceId) { LastFaceId = faceId; }
    }

    public class M18_PlayTwinIntoPlayCard_Tests
    {
        [Fact]
        public void Execute_DelegatesToPlayCard_WithResolvedFaceId_B()
        {
            var resolver = new InMemoryTwinResolver(new() { {(10,1), 11} });
            var spy = new SpyPlayCard();
            var uc = new PlayTwinIntoPlayCard(resolver, spy);
            uc.Execute(new PlayTwin(10, 1));
            Assert.Equal(11, spy.LastFaceId);
        }

        [Fact]
        public void Execute_DelegatesToPlayCard_WithResolvedFaceId_A()
        {
            var resolver = new InMemoryTwinResolver(new() { {(20,0), 19} });
            var spy = new SpyPlayCard();
            var uc = new PlayTwinIntoPlayCard(resolver, spy);
            uc.Execute(new PlayTwin(20, 0));
            Assert.Equal(19, spy.LastFaceId);
        }
    }
}
