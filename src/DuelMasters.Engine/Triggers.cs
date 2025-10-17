namespace DuelMasters.Engine;

public enum TriggerKind { OnCreatureDestroyed }

public sealed record TriggeredAbility(PlayerId Controller, TriggerKind Kind, string? Info = null);
