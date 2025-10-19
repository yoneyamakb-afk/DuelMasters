using System.Collections.Immutable;

namespace DMRules.Engine;

public sealed class GameState
{
    public Phase Phase { get; init; }
    public PlayerId ActivePlayer { get; init; }
    public ImmutableStack<IGameAction> Stack { get; init; }
    public ImmutableQueue<ITriggeredAbility> TriggersAP { get; init; }  // Active Player
    public ImmutableQueue<ITriggeredAbility> TriggersNAP { get; init; } // Non-Active Player
    public int BattlefieldCount { get; init; }
    public int GraveyardCount { get; init; }
    public bool IsLegal { get; init; }
    public bool IsTerminal { get; init; }

    public GameState(
        Phase phase,
        PlayerId activePlayer = PlayerId.P0,
        ImmutableStack<IGameAction>? stack = null,
        ImmutableQueue<ITriggeredAbility>? triggersAP = null,
        ImmutableQueue<ITriggeredAbility>? triggersNAP = null,
        int battlefieldCount = 0,
        int graveyardCount = 0,
        bool isLegal = true,
        bool isTerminal = false)
    {
        Phase = phase;
        ActivePlayer = activePlayer;
        Stack = stack ?? ImmutableStack<IGameAction>.Empty;
        TriggersAP = triggersAP ?? ImmutableQueue<ITriggeredAbility>.Empty;
        TriggersNAP = triggersNAP ?? ImmutableQueue<ITriggeredAbility>.Empty;
        BattlefieldCount = battlefieldCount;
        GraveyardCount = graveyardCount;
        IsLegal = isLegal;
        IsTerminal = isTerminal;
    }

    public static GameState CreateDefault() => new(Phase.Setup);

    public PlayerId NonActivePlayer => ActivePlayer == PlayerId.P0 ? PlayerId.P1 : PlayerId.P0;

    public GameState With(
        Phase? phase = null,
        PlayerId? activePlayer = null,
        ImmutableStack<IGameAction>? stack = null,
        ImmutableQueue<ITriggeredAbility>? triggersAP = null,
        ImmutableQueue<ITriggeredAbility>? triggersNAP = null,
        int? battlefieldCount = null,
        int? graveyardCount = null,
        bool? isLegal = null,
        bool? isTerminal = null)
    {
        return new GameState(
            phase ?? Phase,
            activePlayer ?? ActivePlayer,
            stack ?? Stack,
            triggersAP ?? TriggersAP,
            triggersNAP ?? TriggersNAP,
            battlefieldCount ?? BattlefieldCount,
            graveyardCount ?? GraveyardCount,
            isLegal ?? IsLegal,
            isTerminal ?? IsTerminal
        );
    }
}
