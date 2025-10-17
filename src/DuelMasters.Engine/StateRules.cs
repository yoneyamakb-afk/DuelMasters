using System.Collections.Immutable;
using System.Linq;

namespace DuelMasters.Engine;

public interface IStateRule
{
    GameState Apply(GameState s);
}

public static class StateBasedActions
{
    public static GameState Fix(GameState s)
    {
        var before = s;
        while (true)
        {
            var after = new DestroyZeroPowerRule().Apply(before);
            if (after.Equals(before)) return after;
            before = after;
        }
    }
}

public sealed class DestroyZeroPowerRule : IStateRule
{
    private const int DefaultBasePower = 1000;
    public GameState Apply(GameState s)
    {
        if (s.GameOverResult is not null) return s;
        var players = s.Players;
        bool changed = false;
        for (int pid = 0; pid < players.Length; pid++)
        {
            var p = players[pid];
            if (p.Battle.Cards.Length == 0) continue;

            var cards = p.Battle.Cards.ToBuilder();
            var ids = p.BattleIds.ToBuilder();
            var gy = p.Graveyard.Cards.ToBuilder();

            var removes = new System.Collections.Generic.List<int>();
            for (int i = 0; i < cards.Count; i++)
            {
                int inst = ids[i];
                int buffs = s.ContinuousEffects.Where(e => e.InstanceId == inst && e.ExpiresTurnNumber >= s.TurnNumber).Sum(e => e.Amount);
                int basePower = s.CardDb?.GetBasePower(cards[i]) ?? DefaultBasePower;
                if (basePower + buffs <= 0) removes.Add(i);
            }
            if (removes.Count > 0)
            {
                changed = true;
                foreach (var idx in removes.OrderByDescending(x=>x))
                {
                    var cid = cards[idx];
                    gy.Add(cid);
                    cards.RemoveAt(idx);
                    ids.RemoveAt(idx);
                    s = s with { PendingTriggers = s.PendingTriggers.Add(new TriggeredAbility(new PlayerId(pid), TriggerKind.OnCreatureDestroyed, $"cid={cid.Value}")) };
                }
                var np = p with
                {
                    Battle = new Zone(ZoneKind.Battle, cards.ToImmutable()),
                    BattleIds = ids.ToImmutable(),
                    Graveyard = new Zone(ZoneKind.Graveyard, gy.ToImmutable())
                };
                players = players.SetItem(pid, np);
            }
        }
        return changed ? s with { Players = players } : s;

    }
}
