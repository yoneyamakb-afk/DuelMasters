
using System.Collections.Immutable;

namespace DuelMasters.Engine;

public sealed record PowerBuff(PlayerId Controller, int InstanceId, int Amount, int ExpiresTurnNumber);

public static class EffectUtils
{
    public static ImmutableArray<PowerBuff> RemoveExpired(this ImmutableArray<PowerBuff> effects, int currentTurn)
    {
        var builder = effects.ToBuilder();
        for (int i = builder.Count - 1; i >= 0; i--)
        {
            if (builder[i].ExpiresTurnNumber < currentTurn)
                builder.RemoveAt(i);
        }
        return builder.ToImmutable();
    }
}
