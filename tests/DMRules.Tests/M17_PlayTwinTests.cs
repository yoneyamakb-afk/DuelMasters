using System.Collections.Generic;
using Xunit;
using DMRules.Engine.Events;
using DMRules.Engine.Handlers;
using DMRules.Engine.Services;

namespace DMRules.Tests.M17
{
    file sealed class InMemoryTwinResolver : ITwinFaceResolver
    {
        private readonly Dictionary<(int source, int side), int> _map;
        public InMemoryTwinResolver(Dictionary<(int,int), int> map) => _map = map;
        public int ResolveFaceId(int sourceFaceId, int sideToPlay)
            => _map[(sourceFaceId, sideToPlay)];
    }

    public class M17_PlayTwin_MinimalTests
    {
        [Fact]
        public void PlayTwin_B_Side_ReturnsFaceId()
        {
            var resolver = new InMemoryTwinResolver(new() { {(1,1), 2} });
            var faceId = PlayTwinHandler.Handle(new PlayTwin(1, 1), resolver);
            Assert.Equal(2, faceId);
        }

        [Fact]
        public void PlayTwin_A_Side_ReturnsFaceId()
        {
            var resolver = new InMemoryTwinResolver(new() { {(2,0), 1} });
            var faceId = PlayTwinHandler.Handle(new PlayTwin(2, 0), resolver);
            Assert.Equal(1, faceId);
        }
    }
}