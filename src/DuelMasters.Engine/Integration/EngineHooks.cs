using System;

namespace DuelMasters.Engine.Integration.M11
{
    public static class EngineHooks
    {
        public static IRegulationAdapter? RegulationAdapter { get; set; }

        public static Action<EngineEvent>? OnEvent { get; set; } =
            ev => { RegulationAdapter?.OnEvent(ev); };

        public static void ApplyCardStaticFlags(object gameStateOrContext)
        {
            // no-op for M11.5
        }
    }
}
