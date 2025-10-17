
using System;

namespace DuelMasters.Engine;

public enum TargetKind
{
    None,
    OpponentShield,
    OpponentCreature,
    OwnCreature,
}

public sealed record TargetSpec(TargetKind Kind, int? Index = null)
{
    public static TargetSpec None => new(TargetKind.None, null);
}

public static class Targeting
{
    public static bool IsLegal(GameState s, PlayerId player, TargetSpec spec)
    {
        // Minimal legality: OpponentShield with valid index
        if (spec.Kind == TargetKind.OpponentShield && spec.Index is int i)
        {
            var opp = player.Opponent();
            var shields = s.Players[opp.Value].Shield.Cards;
            return i >= 0 && i < shields.Length;
        }
        if (spec.Kind == TargetKind.OpponentCreature && spec.Index is int ci)
        {
            var opp = player.Opponent();
            var bz = s.Players[opp.Value].Battle.Cards;
            return ci >= 0 && ci < bz.Length;
        }
        if (spec.Kind == TargetKind.OwnCreature && spec.Index is int oi)
        {
            var bz = s.Players[player.Value].Battle.Cards;
            return oi >= 0 && oi < bz.Length;
        }
        return spec.Kind == TargetKind.None;
    }
}
