using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DuelMasters.Engine;

public sealed class Simulator
{
    private readonly ICardDatabase? _db;

    public Simulator(ICardDatabase? db = null) { _db = db; }

    public GameState InitialState(Deck a, Deck b, int seed)
    {
        var s = GameState.Create(a, b, seed);
        if (_db is not null) s = s.With(cardDb: _db);
        return s;
    }

    public bool IsTerminal(GameState s, out GameResult result)
    {
        if (s.GameOverResult is GameResult gr) { result = gr; return true; }
        result = GameResult.InProgress; return false;
    }

    public IEnumerable<ActionIntent> Legal(GameState s) => s.GenerateLegalActions(s.PriorityPlayer);

    public GameState Step(GameState state, ActionIntent intent) => state.Apply(intent);

    public GameState Apply(GameState s, ActionIntent intent) => s.Apply(intent);
}
