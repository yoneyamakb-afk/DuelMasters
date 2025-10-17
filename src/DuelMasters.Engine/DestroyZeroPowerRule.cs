
using System.Linq;
using System.Collections.Immutable;

namespace DuelMasters.Engine;

public sealed class DestroyZeroPowerRule : IStateRule
{
    private const int DefaultBasePower = 1000;

    public GameState Apply(GameState s)
    {
        // If game already over, noop
        if (s.GameOverResult is not null) return s;

        var newPlayers = s.Players;
        bool changed = false;

        for (int pid = 0; pid < newPlayers.Length; pid++)
        {
            var p = newPlayers[pid];
            var bz = p.Battle.Cards;
            if (bz.Length == 0) continue;

            var ids = p.BattleIds;
            var toRemove = ImmutableArray.CreateBuilder<int>();

            for (int i = 0; i < bz.Length; i++)
            {
                int inst = ids[i];
                int buffs = s.ContinuousEffects.Where(e => e.InstanceId == inst && e.Controller.Value == pid && e.ExpiresTurnNumber >= s.TurnNumber).Sum(e => e.Amount);
                var cid = bz[i];
                int basePower = s.CardDb?.GetBasePower(cid) ?? DefaultBasePower;
                int power = basePower + buffs;
                if (power <= 0)
                    toRemove.Add(i);
            }

            if (toRemove.Count > 0)
            {
                changed = true;
                var builderCards = bz.ToBuilder();
                var builderIds = ids.ToBuilder();
                var gy = p.Graveyard.Cards.ToBuilder();
                // Remove highest index first
                foreach (var idx in toRemove.OrderByDescending(x => x))
                {
                    gy.Add(builderCards[idx]);
                    builderCards.RemoveAt(idx);
                    builderIds.RemoveAt(idx);
                }
                var newP = p with
                {
                    Battle = new Zone(ZoneKind.Battle, builderCards.ToImmutable()),
                    BattleIds = builderIds.ToImmutable(),
                    Graveyard = new Zone(ZoneKind.Graveyard, gy.ToImmutable())
                };
                newPlayers = newPlayers.SetItem(pid, newP);
            }
        }

        return changed ? s.With(players: newPlayers) : s;
    }
}
