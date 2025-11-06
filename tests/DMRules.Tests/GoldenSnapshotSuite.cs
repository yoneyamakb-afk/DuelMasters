// M15.2b (update mode) - add SNAPSHOT_UPDATE=1 to re-approve snapshots on mismatch
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Microsoft.Data.Sqlite;

namespace DMRules.Tests
{
    public class GoldenSnapshotSuite
    {
        private static readonly string SnapshotsDir =
            Path.Combine(AppContext.BaseDirectory, "__snapshots__");

        private static readonly (string bucket, string like)[] Buckets = new[]
        {
            ("ShieldTrigger", "%シールドトリガー%"),
            ("Optional", "%もよい%"),
            ("Replacement", "%のかわりに%"),
            ("DurationEOT", "%ターンの終わりまで%"),
            ("DuringOppTurn", "%相手の%ターン%"),
            ("DuringSelfTurn", "%自分の%ターン%"),
            ("MaxN", "%最大%"),
            ("Random", "%ランダム%"),
            ("TwinImpact", "%ツインパクト%"),
            ("HyperMode", "%ハイパーモード%"),
            ("CostChange", "%コスト%"),
            ("Kakumei", "%革命%"),
            ("NeoEvo", "%NEO進化%"),
            ("GNeo", "%G-NEO進化%"),
            ("WorldBreaker", "%ブレイカー%"),
            ("GStrike", "%G・ストライク%"),
            ("DDD", "%D・D・D%"),
            ("Charger", "%チャージャー%"),
            ("ReturnHand", "%手札に戻す%"),
            ("Destroy", "%破壊%")
        };

        private record CardRow(long FaceId, string Name, string Text);

        private static IEnumerable<CardRow> QueryDbIfAvailable()
        {
            var dbPath = Environment.GetEnvironmentVariable("DM_DB_PATH");
            if (string.IsNullOrWhiteSpace(dbPath)) dbPath = "Duelmasters.db";
            if (!File.Exists(dbPath)) yield break;

            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();

            foreach (var (bucket, like) in Buckets)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT face_id, COALESCE(cardname, '') AS name, COALESCE(abilitytxt, '') AS text
                    FROM card_face
                    WHERE abilitytxt LIKE $like
                    ORDER BY face_id
                    LIMIT 1;";
                cmd.Parameters.AddWithValue("$like", like);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var faceId = reader.GetInt64(0);
                    var name = reader.GetString(1);
                    var text = reader.GetString(2);
                    if (!string.IsNullOrWhiteSpace(text))
                        yield return new CardRow(faceId, string.IsNullOrWhiteSpace(name) ? $"Face{faceId}" : name, text);
                }
            }
        }

        private static IEnumerable<(string CardName, string Text)> FixedFallback() => new[]
        {
            ("AquaSurfer", "シールドトリガー。相手のクリーチャーを1体選び、持ち主の手札に戻す。"),
            ("GenericOptional", "カードを1枚引いてもよい。"),
            ("GenericReplacement", "このクリーチャーが破壊されるかわりに、墓地に置かれない。"),
            ("GenericEOT", "ターンの終わりまで、パワーを+2000。"),
            ("GenericOppTurn", "相手の次のターン中、攻撃できない。"),
            ("GenericMax", "相手のクリーチャーを最大2体まで選ぶ。"),
            ("GenericRandom", "ランダムに相手の手札を1枚捨てる。"),
            ("GenericTwinImpact", "ツインパクト：このカードは双面カードである。"),
            ("GenericHyper", "ハイパーモード：このクリーチャーは変身する。"),
            ("GenericCost", "自分の呪文のコストを1減らす。"),
            ("GenericKakumei", "革命チェンジ-火のドラゴン。"),
            ("GenericNeo", "NEO進化：自然のクリーチャー1体の上に置いてもよい。"),
            ("GenericGNeo", "G-NEO進化：自然のクリーチャー1体の上に置いてもよい。"),
            ("GenericWorldBreaker", "このクリーチャーはワールド・ブレイカーを得る。"),
            ("GenericGStrike", "G・ストライク：相手の攻撃中に使ってもよい。"),
            ("GenericDDD", "D・D・D：この呪文を3回使ってもよい。"),
            ("GenericCharger", "チャージャー（この呪文を唱えた後、マナゾーンに置く）。"),
            ("GenericReturn", "相手のクリーチャーを1体、持ち主の手札に戻す。"),
            ("GenericDestroy", "相手のクリーチャーを1体破壊する。"),
            ("GenericTap", "相手のクリーチャーを1体タップする。")
        };

        [Fact(DisplayName = "Golden 20 cards snapshot (DB preferred, fallback; supports update mode)")]
        public void GoldenSnapshots_Run()
        {
            Directory.CreateDirectory(SnapshotsDir);

            var rows = QueryDbIfAvailable()
                .Select(r => (CardName: string.IsNullOrWhiteSpace(r.Name) ? $"Face{r.FaceId}" : r.Name, r.Text))
                .ToList();

            if (rows.Count < 20)
            {
                rows = rows.Concat(FixedFallback()).Take(20).ToList();
            }

            Assert.True(rows.Count >= 15, "Need at least 15 cards to form golden set.");

            int created = 0, verified = 0, updated = 0;
            var update = (Environment.GetEnvironmentVariable("SNAPSHOT_UPDATE") ?? "").Trim() == "1";

            foreach (var (cardName, text) in rows.Take(20))
            {
                var parsed = DMRules.Engine.TextParsing.CardTextParser.Parse(text);
                var tokens = parsed.Tokens.Select(t => t.ToString()).ToArray();
                var unresolved = parsed.UnresolvedPhrases.ToArray();

                var actual = System.Text.Json.JsonSerializer.Serialize(new {
                    CardName = cardName,
                    Tokens = tokens,
                    Unresolved = unresolved
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                var snapPath = Path.Combine(SnapshotsDir, $"{Sanitize(cardName)}.json");

                if (!File.Exists(snapPath))
                {
                    File.WriteAllText(snapPath, actual);
                    created++;
                    continue;
                }

                var expected = File.ReadAllText(snapPath);
                if (Normalize(expected) != Normalize(actual))
                {
                    if (update)
                    {
                        File.WriteAllText(snapPath, actual);
                        updated++;
                        continue;
                    }
                }
                Assert.Equal(Normalize(expected), Normalize(actual));
                verified++;
            }

            Assert.True(created + verified + updated > 0);
        }

        private static string Sanitize(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        private static string Normalize(string s) => s.Replace("\r\n", "\n");
    }
}
