
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using DMRules.Engine;

namespace DMRules.Tests;

public sealed record ScenarioRow(
    string ScenarioId, string Description, string InitialStateJson, string ActionsJson,
    int ExpectedStackAfterSBA, string ExpectedTriggerOrderCsv, bool ExpectSingleReplacement);

public static class CsvScenarios
{
    public static IReadOnlyList<ScenarioRow> Load(string path)
    {
        using var reader = new StreamReader(path);
        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
        using var csv = new CsvReader(reader, cfg);
        return csv.GetRecords<ScenarioRow>().ToList();
    }
}

public static class Adapter { public static IEngineAdapter Instance { get; set; } = new EngineAdapter(); }
