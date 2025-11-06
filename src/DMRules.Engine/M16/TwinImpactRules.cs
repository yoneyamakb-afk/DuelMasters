
// src/DMRules.Engine/M16/TwinImpactRules.cs
// Play-time side selection for TwinImpact cards.
// Usage: call TwinImpactRules.SelectSideOnPlay(...) from your play handler.
using System;

namespace DMRules.Engine.M16
{
    public static class TwinImpactRules
    {
        /// <summary>
        /// Selects the face (A or B) to use when the card is played.
        /// If uiSelectSide is null, defaults to A to preserve backward compatibility.
        /// </summary>
        public static CardFaceInfo SelectSideOnPlay(
            string twinId,
            Func<CardSide?> uiSelectSide = null)
        {
            var side = uiSelectSide?.Invoke() ?? CardSide.A;
            return new CardFaceInfo(twinId ?? string.Empty, side);
        }
    }
}
