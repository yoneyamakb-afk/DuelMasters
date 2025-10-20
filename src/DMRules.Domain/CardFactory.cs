using DMRules.Data;

namespace DMRules.Domain;

public static class CardFactory
{
    public static Card FromRecord(CardRecord r)
    {
        var name = r.CardName ?? "(unknown)";
        var civs = TextNormalization.NormalizeCivs(r.CivilTxt);
        var types = TextNormalization.NormalizeTypes(r.TypeTxt);
        var cost = TextNormalization.ParseIntLoose(r.CostTxt);
        var power = TextNormalization.ParseIntLoose(r.PowerTxt);

        return new Card
        {
            Name = name,
            Cost = cost,
            Power = power,
            Civilizations = civs,
            Types = types,
            RawCivilTxt = r.CivilTxt,
            RawTypeTxt = r.TypeTxt,
            RawCostTxt = r.CostTxt,
            RawPowerTxt = r.PowerTxt,
            FaceId = r.FaceId
        };
    }
}
