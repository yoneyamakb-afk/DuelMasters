
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace DuelMasters.Cards;

public sealed record CardData(
    string id, string name, string[] type, string[] civilization,
    int cost, int? power, string[]? races,
    object[]? abilities);

public static class CardDatabase
{
    public static IReadOnlyDictionary<string, CardData> LoadAll()
    {
        var asm = Assembly.GetExecutingAssembly();
        var res = asm.GetManifestResourceNames().Where(n => n.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
        var dict = new Dictionary<string, CardData>();
        foreach (var name in res)
        {
            using var s = asm.GetManifestResourceStream(name)!;
            using var sr = new StreamReader(s);
            var json = sr.ReadToEnd();
            var cd = JsonSerializer.Deserialize<CardData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (cd is null) continue;
            dict[cd.id] = cd;
        }
        return dict;
    }
}
