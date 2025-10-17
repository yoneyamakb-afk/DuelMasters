namespace DuelMasters.Engine;

public sealed record PowerBuff(PlayerId Controller, int InstanceId, int Amount, int ExpiresTurnNumber);
