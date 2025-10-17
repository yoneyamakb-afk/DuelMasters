namespace DuelMasters.Engine;

public enum ActionType
{
    PassPriority,
    SummonDummyFromHand,
    BuffOwnCreature,
    DestroyOpponentCreature,
    ResolveTop
}

public sealed record ActionIntent(ActionType Type, int Param = -1);
