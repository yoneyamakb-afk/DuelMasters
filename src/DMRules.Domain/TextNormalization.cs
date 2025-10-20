using System.Text.RegularExpressions;

namespace DMRules.Domain;

public static class TextNormalization
{
    // Split multi-value text by common delimiters (Japanese & ASCII)
    private static readonly char[] Delims = new[] { ',', '/', '・', '　', ' ', '|', ';', '、', '／' };

    public static int? ParseIntLoose(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        // common '—' or '-' or '∞' treat as null
        if (s == "-" || s == "—" || s == "―" || s == "∞") return null;
        // pick largest integer in the text
        var m = Regex.Matches(s, @"\d+");
        if (m.Count == 0) return null;
        int max = 0;
        foreach (Match mm in m)
        {
            if (int.TryParse(mm.Value, out var v) && v > max) max = v;
        }
        return max == 0 ? (int?)null : max;
    }

    public static IReadOnlyList<string> SplitMulti(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return Array.Empty<string>();
        return s.Split(Delims, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
    }

    public static string NormalizeCivilization(string civ)
    {
        // map common Japanese labels to canonical English keys
        return civ switch
        {
            "光" or "光文明" => "Light",
            "水" or "水文明" => "Water",
            "闇" or "闇文明" => "Darkness",
            "火" or "火文明" => "Fire",
            "自然" or "自然文明" => "Nature",
            "ゼロ" or "ゼロ文明" or "無色" or "ゼロ文明(無色)" => "Zero",
            _ => civ
        };
    }

    public static string NormalizeType(string t)
    {
        return t switch
        {
            "クリーチャー" => "Creature",
            "呪文" => "Spell",
            "クロスギア" => "Cross Gear",
            "フォートレス" => "Fortress",
            "ドラグハート・クリーチャー" => "Dragheart Creature",
            "ドラグハート・フォートレス" => "Dragheart Fortress",
            _ => t
        };
    }

    public static IReadOnlyList<string> NormalizeCivs(string? civiltxt)
        => SplitMulti(civiltxt).Select(NormalizeCivilization).Distinct().ToArray();

    public static IReadOnlyList<string> NormalizeTypes(string? typetxt)
        => SplitMulti(typetxt).Select(NormalizeType).Distinct().ToArray();
}
