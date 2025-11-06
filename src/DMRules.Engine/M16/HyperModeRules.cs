
// src/DMRules.Engine/M16/HyperModeRules.cs
// Runtime toggle from A (normal) -> B (Hyper) and back.
// Usage: call ActivateHyper(...) when "ハイパー" conditions are met, and ApplyHyper(...) during state resolution.
using System;

namespace DMRules.Engine.M16
{
    public static class HyperModeRules
    {
        public sealed class HyperState
        {
            public bool IsActive { get; private set; }
            public string TwinId { get; }
            public HyperState(string twinId) { TwinId = twinId ?? string.Empty; }
            public void Activate() => IsActive = true;
            public void Deactivate() => IsActive = false;
        }

        public static void ActivateHyper(HyperState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            state.Activate();
        }

        public static CardFaceInfo ApplyHyper(HyperState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return new CardFaceInfo(state.TwinId, state.IsActive ? CardSide.B : CardSide.A);
        }
    }
}
