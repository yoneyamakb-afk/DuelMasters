
using DMRules.Engine.Events;
using DMRules.Engine.Interfaces;
using DMRules.Engine.Services;

namespace DMRules.Engine.UseCases
{
    public sealed class PlayTwinIntoPlayCard
    {
        private readonly ITwinFaceResolver _resolver;
        private readonly IPlayCardByFaceId _playCard;

        public PlayTwinIntoPlayCard(ITwinFaceResolver resolver, IPlayCardByFaceId playCard)
        {
            _resolver = resolver;
            _playCard = playCard;
        }

        public void Execute(PlayTwin e)
        {
            var faceId = _resolver.ResolveFaceId(e.SourceFaceId, e.SideToPlay);
            _playCard.PlayByFaceId(faceId);
        }
    }
}
