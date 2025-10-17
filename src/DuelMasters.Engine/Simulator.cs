
using System;
using System.Collections.Generic;

namespace DuelMasters.Engine;

public sealed class Simulator : ISimulator
{
    public IGameState InitialState(Deck a, Deck b, int seed) => GameState.Create(a, b, seed);

    public IGameState Step(IGameState s, ActionIntent a) => s.Apply(a);

    public IEnumerable<ActionIntent> Legal(IGameState s) => s.GenerateLegalActions(s.ActivePlayer);

    public bool IsTerminal(IGameState s, out GameResult result)
    {
        // Placeholder terminal check
        result = GameResult.InProgress;
        return false;
    }

    public ulong Hash(IGameState s)
    {
        // Minimal non-cryptographic hash placeholder (replace with Zobrist)
        unchecked
        {
            ulong h = 1469598103934665603UL;
            h ^= (ulong)s.ActivePlayer.Value; h *= 1099511628211UL;
            return h;
        }
    }
}
