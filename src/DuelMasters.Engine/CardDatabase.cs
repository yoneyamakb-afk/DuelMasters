
using System;
using System.Data;
using Microsoft.Data.Sqlite;

namespace DuelMasters.Engine;

public interface ICardDatabase
{
    /// <summary>Return base power for a given card id. If unknown, return null.</summary>
    int? GetBasePower(CardId cardId);
}

public sealed class SqliteCardDatabase : ICardDatabase, IDisposable
{
    private readonly string _dbPath;
    private SqliteConnection? _conn;
    private bool _checkedSchema = false;
    private string? _powerQuery; // resolved SQL

    public SqliteCardDatabase(string dbPath)
    {
        _dbPath = dbPath;
        if (System.IO.File.Exists(dbPath))
        {
            _conn = new SqliteConnection($"Data Source={dbPath}");
            _conn.Open();
        }
    }

    
    public int? GetBasePower(CardId cardId)
    {
        try
        {
            if (_conn is null) return null;
            if (!_checkedSchema) ResolveSchema();
            if (_powerQuery is null) return null;

            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = _powerQuery;
                cmd.Parameters.AddWithValue("@id", cardId.Value);
                object? obj = cmd.ExecuteScalar();
                if (obj != null && obj != DBNull.Value && int.TryParse(Convert.ToString(obj), out var powExact))
                    return powExact;
            }

            using (var cmd2 = _conn.CreateCommand())
            {
                cmd2.CommandText = "SELECT powertxt FROM card_face ORDER BY face_id LIMIT 1 OFFSET @o;";
                cmd2.Parameters.AddWithValue("@o", cardId.Value);
                object? obj2 = cmd2.ExecuteScalar();
                if (obj2 != null && obj2 != DBNull.Value && int.TryParse(Convert.ToString(obj2), out var powIdx))
                    return powIdx;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private void ResolveSchema()
    {
        _checkedSchema = true;
        try
        {
            if (HasTable("card_face") && HasColumn("card_face", "powertxt") && HasColumn("card_face", "face_id"))
            {
                _powerQuery = "SELECT powertxt FROM card_face WHERE face_id = @id LIMIT 1;";
                return;
            }
            if (HasTable("card_master") && HasColumn("card_master", "power") && HasColumn("card_master", "id"))
            {
                _powerQuery = "SELECT power FROM card_master WHERE id = @id LIMIT 1;";
                return;
            }
            _powerQuery = null;
        }
        catch
        {
            _powerQuery = null;
        }
    }

    private bool HasTable(string name)
    {
        using var cmd = _conn!.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@n;";
        cmd.Parameters.AddWithValue("@n", name);
        using var r = cmd.ExecuteReader();
        return r.Read();
    }

    private bool HasColumn(string table, string col)
    {
        using var cmd = _conn!.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table});";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var name = Convert.ToString(r["name"]);
            if (string.Equals(name, col, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    public void Dispose()
    {
        try { _conn?.Dispose(); } catch { }
    }
}
