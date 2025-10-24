
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace DMRules.Effects.Text
{
    /// <summary>
    /// SQLite の Duelmasters.db (table: card_face) から効果テキストを取得する実装。
    /// スキーマ差異に対応するため、候補カラムを列挙し、存在する最初のものを使用します。
    /// 既定候補: effecttxt, text, rulestxt, rawtxt, texttxt, rule_text
    /// </summary>
    public sealed class SqliteEffectTextProvider : IEffectTextProvider, IDisposable
    {
        private readonly string _dbPath;
        private readonly string[] _candidates;
        private readonly SqliteConnection _conn;
        private readonly Lazy<string?> _chosenColumn;

        public SqliteEffectTextProvider(string dbPath, IEnumerable<string>? columnCandidates = null)
        {
            _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
            if (!File.Exists(_dbPath))
                throw new FileNotFoundException("SQLite database not found", _dbPath);

            _candidates = (columnCandidates ?? new[] { "effecttxt", "text", "rulestxt", "rawtxt", "texttxt", "rule_text" })
                .ToArray();

            _conn = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString());
            _conn.Open();

            _chosenColumn = new Lazy<string?>(DetectTextColumn);
        }

        private string? DetectTextColumn()
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(card_face);";
            using var r = cmd.ExecuteReader();
            var columns = new List<string>();
            while (r.Read())
            {
                var name = r.GetString(1);
                columns.Add(name);
            }

            foreach (var c in _candidates)
            {
                var hit = columns.FirstOrDefault(x => string.Equals(x, c, StringComparison.OrdinalIgnoreCase));
                if (hit != null) return hit;
            }

            // フォールバック: 「text」を含む最初の列
            var containsText = columns.FirstOrDefault(x => x.IndexOf("text", StringComparison.OrdinalIgnoreCase) >= 0);
            return containsText; // null 可（→ No-Op になる）
        }

        public string? GetEffectTextByFaceId(int faceId)
        {
            var col = _chosenColumn.Value;
            if (col is null) return null;

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = $"SELECT {QuoteIdent(col)} FROM card_face WHERE face_id = $id LIMIT 1;";
            cmd.Parameters.AddWithValue("$id", faceId);
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? null : result as string;
        }

        public string? GetEffectTextByName(string cardName)
        {
            var col = _chosenColumn.Value;
            if (col is null) return null;

            // name カラム候補（スキーマ差異に備える）
            var nameCols = new[] { "cardname", "name", "card_name", "jpname", "title" };
            var existing = GetExistingColumns("card_face").Intersect(nameCols, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
            if (existing is null) return null;

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = $"SELECT {QuoteIdent(col)} FROM card_face WHERE {QuoteIdent(existing)} = $name LIMIT 1;";
            cmd.Parameters.AddWithValue("$name", cardName);
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? null : result as string;
        }

        private IEnumerable<string> GetExistingColumns(string table)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({QuoteIdent(table)});";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                yield return r.GetString(1);
            }
        }

        private static string QuoteIdent(string ident)
        {
            // SQLite 識別子の簡易クオート（"name"）
            if (ident.Contains('"')) ident = ident.Replace("\"", "\"\"");
            return $"\"{ident}\"";
        }

        public void Dispose() => _conn.Dispose();
    }
}
