
namespace DMRules.Effects;

public static class EffectPlanner
{
    public static PlannedEffect Plan(EffectIR.Effect effect)
        => new(effect);

    public readonly record struct PlannedEffect(EffectIR.Effect Effect)
    {
        public void OnEnterBattlezone(IEffectHost host, int controllerId)
        {
            foreach (var clause in Effect.Clauses)
            {
                if (clause is EffectIR.OnEvent { Trigger: EffectIR.Trigger.EnterBattlezone, Action: var a })
                {
                    Execute(host, controllerId, a);
                }
            }
        }

        public void OnAttackDeclared(IEffectHost host, int controllerId)
        {
            foreach (var clause in Effect.Clauses)
            {
                if (clause is EffectIR.OnEvent { Trigger: EffectIR.Trigger.AttackDeclared, Action: var a })
                {
                    Execute(host, controllerId, a);
                }
            }
        }

        private static void Execute(IEffectHost host, int controllerId, EffectIR.ActionDef action)
        {
            switch (action)
            {
                case EffectIR.Draw d:
                    host.DrawCards(controllerId, d.Cards);
                    break;
                case EffectIR.AddMana m:
                    host.AddMana(controllerId, m.Cards);
                    break;
                case EffectIR.NoOp:
                default:
                    break;
            }
        }
    }
}
