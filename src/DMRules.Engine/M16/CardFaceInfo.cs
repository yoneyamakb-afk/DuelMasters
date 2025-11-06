
// src/DMRules.Engine/M16/CardFaceInfo.cs
// Attach twin_id / side metadata for traces and rules.
namespace DMRules.Engine.M16
{
    public sealed class CardFaceInfo
    {
        public string TwinId { get; }
        public CardSide Side { get; }

        public CardFaceInfo(string twinId, CardSide side)
        {
            TwinId = twinId ?? string.Empty;
            Side = side;
        }
    }
}
