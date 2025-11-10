using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace DuelMasters.GUI.Game;

/// <summary>
/// Duelmasters.db の card_face からカード情報を読み込む簡易DB
/// </summary>
internal sealed class CardDatabase
{
    private sealed class CardRecord
    {
        public int FaceId { get; init; }
        public string CardName { get; init; } = "";
        public string TypeText { get; init; } = "";
        public string CivilText { get; init; } = "";
        public int? Cost { get; init; }
        public int? Power { get; init; }
    }

    private readonly Dictionary<int, CardRecord> _cards;

    private CardDatabase(Dictionary<int, CardRecord> cards)
    {
        _cards = cards;
    }

    /// <summary>
    /// 例外を外に漏らさないロード用。成功時 true。
    /// </summary>
    public static bool TryLoad(string dbPath, out CardDatabase? db)
    {
        db = null;

        try
        {
            if (!File.Exists(dbPath))
            {
                Console.WriteLine($"[CardDB] ファイルが見つかりません: {dbPath}");
                return false;
            }

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadOnly
            };

            using var conn = new SqliteConnection(builder.ToString());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT face_id, cardname, typetxt, civiltxt, costtxt, powertxt
                FROM card_face
                WHERE side = 'A';
            ";

            using var reader = cmd.ExecuteReader();

            var dict = new Dictionary<int, CardRecord>();
            while (reader.Read())
            {
                var faceId = reader.GetInt32(0);
                var cardName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                var typeText = reader.IsDBNull(2) ? "" : reader.GetString(2);
                var civil   = reader.IsDBNull(3) ? "" : reader.GetString(3);
                int? cost   = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4);
                int? power  = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5);

                dict[faceId] = new CardRecord
                {
                    FaceId = faceId,
                    CardName = cardName,
                    TypeText = typeText,
                    CivilText = civil,
                    Cost = cost,
                    Power = power
                };
            }

            db = new CardDatabase(dict);
            Console.WriteLine($"[CardDB] 読み込み完了（{dict.Count} 件）");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CardDB] 読み込みに失敗しました: {ex.Message}");
            db = null;
            return false;
        }
    }

    /// <summary>
    /// デッキ一覧表示などに使うための、人間向けサマリ文字列。
    /// </summary>
    public string GetCardSummary(int faceId)
    {
        if (!_cards.TryGetValue(faceId, out var r))
        {
            return $"ID={faceId} (不明なカード)";
        }

        var costPart = r.Cost.HasValue ? $"コスト{r.Cost.Value}" : "コスト不明";
        var civPart  = string.IsNullOrWhiteSpace(r.CivilText) ? "" : $"／{r.CivilText}";
        var typePart = string.IsNullOrWhiteSpace(r.TypeText) ? "" : r.TypeText;

        // 例: ARC REALITY COMPLEX（NEOクリーチャー／光/水/闇／コスト3）
        return $"{r.CardName}（{typePart}{civPart}／{costPart}）";
    }
}
