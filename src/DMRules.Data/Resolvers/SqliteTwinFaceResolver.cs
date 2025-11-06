using System;
using Microsoft.Data.Sqlite;
using DMRules.Engine.Services;

namespace DMRules.Data.Resolvers
{
    /// <summary>
    /// Duelmasters.db の card_face テーブルを用いて、TwinImpactの相互面FaceIdを解決します。
    /// 前提スキーマ（M16時点の実績に基づく）:
    ///   card_face(face_id INTEGER PRIMARY KEY, twin_id INTEGER NULL, side INTEGER /*0=A,1=B*/)
    /// 既定パス: リポジトリルートの Duelmasters.db
    /// 環境変数 DM_DB_PATH があればそちらを優先します。
    /// </summary>
    public sealed class SqliteTwinFaceResolver : ITwinFaceResolver
    {
        private readonly string _dbPath;

        public SqliteTwinFaceResolver(string? dbPath = null)
        {
            _dbPath = string.IsNullOrWhiteSpace(dbPath)
                ? (Environment.GetEnvironmentVariable("DM_DB_PATH") ?? "Duelmasters.db")
                : dbPath!;
        }

        public int ResolveFaceId(int sourceFaceId, int sideToPlay)
        {
            // 1) sourceFaceId の twin_id を取得
            var twinId = GetTwinId(sourceFaceId);
            if (twinId is null)
                throw new InvalidOperationException($"Face {sourceFaceId} is not a TwinImpact card (twin_id is NULL).");

            // 2) twin_id + side で相手面の face_id を取得
            var targetFaceId = GetFaceIdByTwinAndSide(twinId.Value, sideToPlay);
            if (targetFaceId is null)
                throw new InvalidOperationException($"Twin face not found. twin_id={twinId} side={sideToPlay}");

            return targetFaceId.Value;
        }

        private int? GetTwinId(int faceId)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT twin_id FROM card_face WHERE face_id = $face";
            cmd.Parameters.AddWithValue("$face", faceId);
            var obj = cmd.ExecuteScalar();
            if (obj == null || obj is DBNull) return null;
            return Convert.ToInt32(obj);
        }

        private int? GetFaceIdByTwinAndSide(int twinId, int side)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT face_id FROM card_face WHERE twin_id = $t AND side = $s LIMIT 1";
            cmd.Parameters.AddWithValue("$t", twinId);
            cmd.Parameters.AddWithValue("$s", side);
            var obj = cmd.ExecuteScalar();
            if (obj == null || obj is DBNull) return null;
            return Convert.ToInt32(obj);
        }
    }
}