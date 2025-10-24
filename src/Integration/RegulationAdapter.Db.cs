using System;
using Microsoft.Data.Sqlite;

namespace DuelMasters.Engine.Integration.M11
{
    internal static class RegulationConfig
    {
        public static string ResolveDbPath()
        {
            var env = Environment.GetEnvironmentVariable("DUELMASTERS_DB");
            if (!string.IsNullOrWhiteSpace(env) && System.IO.File.Exists(env)) return env;
            var local = System.IO.Path.Combine(Environment.CurrentDirectory, "Duelmasters.db");
            return local;
        }
        public static string BuildConnectionString(string? dbPath = null)
        {
            dbPath ??= ResolveDbPath();
            return new SqliteConnectionStringBuilder { DataSource = dbPath, Mode = SqliteOpenMode.ReadWriteCreate }.ToString();
        }
    }

    public sealed class DbRegulationAdapter : IRegulationAdapter
    {
        private readonly string _dbPath;
        public DbRegulationAdapter(string? dbPath = null){ _dbPath = dbPath ?? RegulationConfig.ResolveDbPath(); }

        public CardRegulationFlags GetStaticFlags(string cardName, string? setCode = null)
        {
            try
            {
                using var conn = new SqliteConnection(RegulationConfig.BuildConnectionString(_dbPath));
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT flags FROM regulation_flags 
                                        WHERE cardname=$n AND (setcode=$s OR $s IS NULL) 
                                        LIMIT 1;";
                    cmd.Parameters.AddWithValue("$n", cardName);
                    cmd.Parameters.AddWithValue("$s", (object?)setCode ?? DBNull.Value);
                    var o = cmd.ExecuteScalar();
                    if (o != null && o != DBNull.Value) return (CardRegulationFlags)Convert.ToInt32(o);
                }
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT flags FROM regulation_flags 
                                        WHERE cardname=$n 
                                        ORDER BY setcode IS NOT NULL DESC LIMIT 1;";
                    cmd.Parameters.AddWithValue("$n", cardName);
                    var o = cmd.ExecuteScalar();
                    if (o != null && o != DBNull.Value) return (CardRegulationFlags)Convert.ToInt32(o);
                }
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT flags FROM regulation_flags_txt 
                                        WHERE cardname=$n AND (setcode=$s OR $s IS NULL) 
                                        LIMIT 1;";
                    cmd.Parameters.AddWithValue("$n", cardName);
                    cmd.Parameters.AddWithValue("$s", (object?)setCode ?? DBNull.Value);
                    var t = cmd.ExecuteScalar() as string;
                    if (!string.IsNullOrWhiteSpace(t))
                    {
                        CardRegulationFlags res = CardRegulationFlags.None;
                        foreach (var part in t.Split(',', StringSplitOptions.RemoveEmptyEntries))
                            if (Enum.TryParse<CardRegulationFlags>(part.Trim(), true, out var f)) res |= f;
                        return res;
                    }
                }
                return CardRegulationFlags.None;
            }
            catch { return CardRegulationFlags.None; }
        }

        public void OnEvent(EngineEvent ev) { /* optional logging */ }
    }
}
