using System;
using System.Collections.Generic;

namespace DuelMasters.Engine;

public sealed class Simulator
{
    private readonly ICardDatabase? _db;

    public Simulator(ICardDatabase? db = null)
    {
        _db = db;
    }

    public GameState InitialState(Deck a, Deck b, int seed)
    {
        var s = GameState.Create(a, b, seed);
        if (_db is not null)
            s = s.With(cardDb: _db);
        return s;
    }

    // ---- 追加：IsTerminal（従来互換）
    public bool IsTerminal(IGameState s, out GameResult result)
    {
        if (s is GameState gs && gs.GameOverResult is GameResult gr)
        {
            result = gr;
            return true;
        }
        result = GameResult.InProgress;
        return false;
    }

    // オーバーロード（呼び出し元によってはGameStateで受ける場合があるため）
    public bool IsTerminal(GameState s, out GameResult result)
        => IsTerminal((IGameState)s, out result);

    // ---- 追加：Legal（従来互換）
    public IEnumerable<ActionIntent> Legal(GameState s)
        => s.GenerateLegalActions(s.PriorityPlayer);

    // ---- 既存：1手進める（必要なら従来の呼び方を残す）
    public GameState Step(GameState state, ActionIntent intent)
    {
        var after = Apply(state, intent);
        return after;
    }

    // ---- 既存：状態にアクションを適用
    public GameState Apply(GameState s, ActionIntent intent)
        => (GameState)s.Apply(intent);
}
