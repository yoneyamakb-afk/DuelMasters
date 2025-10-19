namespace DMRules.Engine;
public static class StateBasedActions
{
    public static GameState Evaluate(GameState s) => s.AddTrace("SBA", "Evaluate").With(isLegal: s.BattlefieldCount >= 0 && s.GraveyardCount >= 0, isTerminal: false);
}
