using Microsoft.Data.Sqlite;

namespace DuelMasters.Engine;

public interface ICardDatabase
{
    int? GetBasePower(CardId cardId);
}

public sealed class SqliteCardDatabase : ICardDatabase, System.IDisposable
{
    private readonly SqliteConnection? _conn;
    private bool _checked;
    private string? _query;

    public SqliteCardDatabase(string path)
    {
        if (System.IO.File.Exists(path))
        {
            _conn = new SqliteConnection($"Data Source={path}");
            _conn.Open();
        }
    }

    public int? GetBasePower(CardId cardId)
    {
        try
        {
            if (_conn is null) return null;
            if (!_checked) Resolve();
            if (_query is null) return null;

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = _query;
            cmd.Parameters.AddWithValue("@id", cardId.Value);
            var obj = cmd.ExecuteScalar();
            if (obj != null && obj != System.DBNull.Value && int.TryParse(System.Convert.ToString(obj), out var pow))
                return pow;

            using var cmd2 = _conn.CreateCommand();
            cmd2.CommandText = "SELECT powertxt FROM card_face ORDER BY face_id LIMIT 1 OFFSET @o;";
            cmd2.Parameters.AddWithValue("@o", cardId.Value);
            var obj2 = cmd2.ExecuteScalar();
            if (obj2 != null && obj2 != System.DBNull.Value && int.TryParse(System.Convert.ToString(obj2), out var pow2))
                return pow2;

            return null;
        }
        catch { return null; }
    }

    private void Resolve()
    {
        _checked = true;
        try
        {
            if (HasTable("card_face") && HasCol("card_face","powertxt") && HasCol("card_face","face_id"))
            { _query = "SELECT powertxt FROM card_face WHERE face_id = @id LIMIT 1;"; return; }
            _query = null;
        } catch { _query = null; }
    }
    private bool HasTable(string name)
    {
        using var c = _conn!.CreateCommand();
        c.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@n;";
        c.Parameters.AddWithValue("@n", name);
        using var r = c.ExecuteReader();
        return r.Read();
    }
    private bool HasCol(string table, string col)
    {
        using var c = _conn!.CreateCommand();
        c.CommandText = $"PRAGMA table_info({table});";
        using var r = c.ExecuteReader();
        while (r.Read())
        {
            var n = System.Convert.ToString(r["name"]);
            if (string.Equals(n, col, System.StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    public void Dispose() { try { _conn?.Dispose(); } catch {} }
}
