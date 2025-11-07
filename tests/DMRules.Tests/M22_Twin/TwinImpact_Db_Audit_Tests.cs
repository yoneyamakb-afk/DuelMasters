
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;
using Xunit;
using DMRules.Tests.Snapshots;

namespace DMRules.Tests.M22
{
    public class TwinImpact_Db_Audit_Tests
    {
        private static string? FindDbPath()
        {
            var env = Environment.GetEnvironmentVariable("DM_DB_PATH");
            if (!string.IsNullOrWhiteSpace(env) && File.Exists(env)) return env;
            var candidates = new[] {
                Path.Combine(AppContext.BaseDirectory, "..","..","..","..","Duelmasters.db"),
                Path.Combine(AppContext.BaseDirectory, "..","..","..","Duelmasters.db"),
                Path.Combine(AppContext.BaseDirectory, "Duelmasters.db"),
                Path.Combine(Directory.GetCurrentDirectory(), "Duelmasters.db")
            };
            foreach (var c in candidates)
                if (File.Exists(c)) return Path.GetFullPath(c);
            return null;
        }

        private static bool TableHasColumns(SqliteConnection conn, string table, params string[] cols)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({table});";
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                set.Add(r.GetString(1));
            foreach (var c in cols)
                if (!set.Contains(c)) return false;
            return true;
        }

        [Fact]
        public void TwinImpact_Data_Integrity_Snapshot()
        {
            var dbPath = FindDbPath();
            if (dbPath is null)
            {
                SnapshotAssert.MatchJson("m22_db_audit", new { db_not_found = true });
                return;
            }

            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();

            if (!TableHasColumns(conn, "card_face", "face_id", "twin_id", "side"))
            {
                SnapshotAssert.MatchJson("m22_db_audit", new { db_path = dbPath, schema_ok = false });
                return;
            }

            var payload = new Dictionary<string, object?>();
            payload["db_path"] = dbPath;
            payload["schema_ok"] = true;

            var groups = new List<Dictionary<string, object?>>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT twin_id,
                           SUM(CASE WHEN side=0 THEN 1 ELSE 0 END) AS a_count,
                           SUM(CASE WHEN side=1 THEN 1 ELSE 0 END) AS b_count,
                           COUNT(*) AS total
                    FROM card_face
                    WHERE twin_id IS NOT NULL
                    GROUP BY twin_id
                    ORDER BY twin_id;";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    groups.Add(new Dictionary<string, object?>{
                        ["twin_id"] = r.IsDBNull(0) ? null : r.GetInt32(0),
                        ["a_count"] = r.IsDBNull(1) ? 0 : r.GetInt32(1),
                        ["b_count"] = r.IsDBNull(2) ? 0 : r.GetInt32(2),
                        ["total"]   = r.IsDBNull(3) ? 0 : r.GetInt32(3),
                    });
                }
            }
            payload["twin_groups"] = groups;

            var anomalies = new List<Dictionary<string, object?>>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT twin_id,
                           SUM(CASE WHEN side=0 THEN 1 ELSE 0 END) AS a_count,
                           SUM(CASE WHEN side=1 THEN 1 ELSE 0 END) AS b_count,
                           COUNT(*) AS total
                    FROM card_face
                    WHERE twin_id IS NOT NULL
                    GROUP BY twin_id
                    HAVING (a_count <> 1 OR b_count <> 1 OR total <> 2)
                    ORDER BY twin_id;";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    anomalies.Add(new Dictionary<string, object?>{
                        ["twin_id"] = r.IsDBNull(0) ? null : r.GetInt32(0),
                        ["a_count"] = r.IsDBNull(1) ? 0 : r.GetInt32(1),
                        ["b_count"] = r.IsDBNull(2) ? 0 : r.GetInt32(2),
                        ["total"]   = r.IsDBNull(3) ? 0 : r.GetInt32(3),
                    });
                }
            }
            payload["anomalies"] = anomalies;

            var faces = new List<Dictionary<string, object?>>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT twin_id, side, face_id
                    FROM card_face
                    WHERE twin_id IS NOT NULL
                    ORDER BY twin_id, side, face_id;";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    faces.Add(new Dictionary<string, object?>{
                        ["twin_id"] = r.IsDBNull(0) ? null : r.GetInt32(0),
                        ["side"]    = r.IsDBNull(1) ? null : r.GetInt32(1),
                        ["face_id"] = r.IsDBNull(2) ? null : r.GetInt32(2),
                    });
                }
            }
            payload["faces"] = faces;

            SnapshotAssert.MatchJson("m22_db_audit", payload);
        }
    }
}
