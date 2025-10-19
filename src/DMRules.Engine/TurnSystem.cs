namespace DMRules.Engine;

public static class TurnSystem
{
    public static GameState AdvancePhase(GameState s)
    {
        return s.Phase switch
        {
            Phase.Setup => s.With(phase: Phase.StartOfTurn),
            Phase.StartOfTurn => s.With(phase: Phase.Main),
            Phase.Main => s.With(phase: Phase.Attack),
            Phase.Attack => s.With(phase: Phase.End),
            Phase.End => s.With(phase: Phase.StartOfTurn, activePlayer: s.NonActivePlayer),
            _ => s
        };
    }
}
