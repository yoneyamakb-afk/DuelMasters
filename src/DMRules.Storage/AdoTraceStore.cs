using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using DMRules.Engine;

namespace DMRules.Storage
{
    /// <summary>
    /// Saves TraceEntry rows to a table using plain ADO.NET.
    /// Requires a working IDbConnectionFactory (e.g., SqlClient, Npgsql, Sqlite).
    /// </summary>
    public sealed class AdoTraceStore : ITraceStore
    {
        private readonly IDbConnectionFactory _factory;
        private readonly string _tableName;

        public AdoTraceStore(IDbConnectionFactory factory, string tableName = "TraceEntries")
        {
            _factory = factory;
            _tableName = tableName;
        }

        public async Task SaveAsync(IEnumerable<TraceEntry> trace, CancellationToken ct = default)
        {
            // ADO.NET is sync API; wrap with Task.Run to keep async signature friendly to ASP/CLI.
            await Task.Run(() =>
            {
                using var conn = _factory.Create();
                conn.Open();

                EnsureSchema(conn);

                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"INSERT INTO {_tableName} (Ordinal, Kind, Detail, CreatedAtUtc) VALUES (@o, @k, @d, @ts)";
                var pOrdinal = cmd.CreateParameter(); pOrdinal.ParameterName = "@o"; cmd.Parameters.Add(pOrdinal);
                var pKind = cmd.CreateParameter();    pKind.ParameterName = "@k"; cmd.Parameters.Add(pKind);
                var pDetail = cmd.CreateParameter();  pDetail.ParameterName = "@d"; cmd.Parameters.Add(pDetail);
                var pTs = cmd.CreateParameter();      pTs.ParameterName = "@ts"; cmd.Parameters.Add(pTs);

                foreach (var t in trace)
                {
                    ct.ThrowIfCancellationRequested();
                    pOrdinal.Value = t.Ordinal;
                    pKind.Value = t.Kind ?? string.Empty;
                    pDetail.Value = t.Detail ?? string.Empty;
                    pTs.Value = DateTime.UtcNow;
                    cmd.ExecuteNonQuery();
                }
            }, ct);
        }

        private void EnsureSchema(IDbConnection conn)
        {
            using var cmd = conn.CreateCommand();
            // Minimal portable DDL. Many RDBMS accept this with slight variations; adjust if needed.
            cmd.CommandText = $@"
CREATE TABLE IF NOT EXISTS {_tableName} (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Ordinal BIGINT NOT NULL,
  Kind TEXT NOT NULL,
  Detail TEXT NOT NULL,
  CreatedAtUtc DATETIME NOT NULL
);
";
            try { cmd.ExecuteNonQuery(); } catch { /* some providers lack IF NOT EXISTS; ignore */ }
        }
    }
}
