namespace DMRules.Engine;

public static class StateBasedActions
{
    public static GameState Evaluate(GameState s)
    {
        var legal = s.BattlefieldCount >= 0 && s.GraveyardCount >= 0;
        var terminal = false; // placeholder
        return s.With(isLegal: legal, isTerminal: terminal);
    }
}
