using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace DMRules.Tests.M24
{
    public class TraceSnapshot_MinimalTests
    {
        [Fact]
        public void Minimal_Trace_Is_Stable_After_Normalize()
        {
            var actualRaw = JsonSerializer.Serialize(new {
                timestamp = "2025-11-07T01:23:45.678Z",
                traceId = "2f1b6c58-0c9b-4d2f-97fd-8ff7c0a3fb97",
                twin_id = 111, side = "B",
                card_id = 222, face_id = 333,
                id = "AbC123xYz9", ptr = "0xABCDEF"
            });

            string Normalize(string s)
            {
                s = Regex.Replace(s, @"\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}:\d{2}(?:\.\d+)?Z?", "<T>");
                s = Regex.Replace(s, @"\b[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}\b", "<GUID>");
                s = Regex.Replace(s, @"\b0x[0-9a-fA-F]+\b", "<HEX>");
                s = Regex.Replace(s, "(?<=\\\"(id|eventId|stackId)\\\"\\s*:\\s*\\\")[^\\\"]+(?=\\\")", "<ID>", RegexOptions.IgnoreCase);
                return s;
            }

            var actual = Normalize(actualRaw);

            var golden = "{\"card_id\":222,\"face_id\":333,\"id\":\"<ID>\",\"ptr\":\"<HEX>\",\"side\":\"B\",\"timestamp\":\"<T>\",\"traceId\":\"<GUID>\",\"twin_id\":111}";

            var actualJson = JsonDocument.Parse(actual).RootElement;
            var goldenJson = JsonDocument.Parse(golden).RootElement;

            Assert.True(JsonElementEquality(goldenJson, actualJson));
        }

        private static bool JsonElementEquality(JsonElement a, JsonElement b)
        {
            if (a.ValueKind != b.ValueKind) return false;
            switch (a.ValueKind)
            {
                case JsonValueKind.Object:
                    var aProps = a.EnumerateObject().OrderBy(p => p.Name).ToArray();
                    var bProps = b.EnumerateObject().OrderBy(p => p.Name).ToArray();
                    if (aProps.Length != bProps.Length) return false;
                    for (int i = 0; i < aProps.Length; i++)
                    {
                        if (aProps[i].Name != bProps[i].Name) return false;
                        if (!JsonElementEquality(aProps[i].Value, bProps[i].Value)) return false;
                    }
                    return true;

                case JsonValueKind.Array:
                    var aArr = a.EnumerateArray().ToArray();
                    var bArr = b.EnumerateArray().ToArray();
                    if (aArr.Length != bArr.Length) return false;
                    for (int i = 0; i < aArr.Length; i++)
                    {
                        if (!JsonElementEquality(aArr[i], bArr[i])) return false;
                    }
                    return true;

                default:
                    return a.ToString() == b.ToString();
            }
        }
    }
}
