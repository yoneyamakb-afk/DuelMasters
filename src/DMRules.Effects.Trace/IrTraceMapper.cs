
using System.Collections.Generic;
using DMRules.Effects;

namespace DMRules.Effects.Trace
{
    public static class IrTraceMapper
    {
        public static List<EffectTraceAction> ExtractActions(EffectIR.Effect eff, EffectIR.Trigger trigger)
        {
            var list = new List<EffectTraceAction>();
            foreach (var c in eff.Clauses)
            {
                if (c is EffectIR.OnEvent { Trigger: var t, Action: var a } && t == trigger)
                {
                    switch (a)
                    {
                        case EffectIR.Draw d:
                            list.Add(new EffectTraceAction("Draw", d.Cards));
                            break;
                        case EffectIR.AddMana m:
                            list.Add(new EffectTraceAction("AddMana", m.Cards));
                            break;
                        default:
                            list.Add(new EffectTraceAction(a.GetType().Name, null));
                            break;
                    }
                }
            }
            return list;
        }
    }
}
