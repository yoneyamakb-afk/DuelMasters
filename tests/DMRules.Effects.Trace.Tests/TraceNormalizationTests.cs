using System.Text.RegularExpressions;
using Xunit;

// 簡易インライン正規化（本体に触らずテストで確認できる版）
static class _InlineTraceNormalizer
{
    static readonly Regex IsoTime = new Regex(
        @"\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}:\d{2}(?:\.\d+)?Z?",
        RegexOptions.Compiled);

    static readonly Regex Guid = new Regex(
        @"\b[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}\b",
        RegexOptions.Compiled);

    static readonly Regex HexLike = new Regex(@"\b0x[0-9a-fA-F]+\b", RegexOptions.Compiled);

    // traceId は GUID 置換の対象にしたいので、ここでは除外する
    static readonly Regex JsonIdValue = new Regex(
        "(?<=\\\"(id|eventId|stackId)\\\"\\s*:\\s*\\\")[^\\\"]+(?=\\\")",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // まず一般的なID（traceId除外）を <ID> に
        var s = JsonIdValue.Replace(input, "<ID>");
        // その後、時刻・GUID・HEX を個別に置換
        s = IsoTime.Replace(s, "<T>");
        s = Guid.Replace(s, "<GUID>");
        s = HexLike.Replace(s, "<HEX>");
        return s;
    }
}

public class TraceNormalizationTests
{
    [Fact]
    public void Normalize_Masks_Volatiles_And_Keeps_TwinAndSide_Fields()
    {
        var raw = @"{
          ""timestamp"": ""2025-11-07T06:12:34.567Z"",
          ""traceId"": ""de305d54-75b4-431b-adb2-eb6b9e546014"",
          ""twin_id"": 12345,
          ""side"": ""A"",
          ""card_id"": 6789,
          ""face_id"": 42,
          ""id"": ""abcDEF1234"",
          ""ptr"": ""0x7ffeefbff5c0""
        }";

        var got = _InlineTraceNormalizer.Normalize(raw);

        Assert.Contains(@"""twin_id"": 12345", got); // 保持
        Assert.Contains(@"""side"": ""A""", got);    // 保持
        Assert.Contains(@"""card_id"": 6789", got);  // 保持
        Assert.Contains(@"""face_id"": 42", got);    // 保持

        Assert.Contains(@"""timestamp"": ""<T>""", got);
        Assert.Contains(@"""traceId"": ""<GUID>""", got);
        Assert.Contains(@"""ptr"": ""<HEX>""", got);
    }
}
