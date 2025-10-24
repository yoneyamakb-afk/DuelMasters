using System;

namespace DuelMasters.Engine.Integration.M11
{
    public interface IRegulationAdapter
    {
        CardRegulationFlags GetStaticFlags(string cardName, string? setCode = null);
        void OnEvent(EngineEvent ev);
    }

    [Flags]
    public enum CardRegulationFlags
    {
        None     = 0,
        Limited1 = 1 << 0,
        Limited0 = 1 << 1,
        PromoOnly= 1 << 2,
        Errata   = 1 << 3
    }
}
