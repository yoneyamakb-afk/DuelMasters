using Microsoft.Data.Sqlite;

namespace DMRules.Data;

public sealed class CardRepository
{
    private readonly string _dbPath;
    private readonly string _tableName;

    public CardRepository(string? dbPath = null, string tableName = "card_face")
    {
        _dbPath = dbPath ?? AppConfig.GetDatabasePath();
        _tableName = tableName;
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        return conn;
    }

    public IReadOnlyList<CardRecord> GetAll(int? limit = null, int? offset = null)
    {
        using var conn = Open();
        var sql = $"SELECT * FROM '{_tableName}'";
        if (limit.HasValue)
        {
            sql += " LIMIT @limit";
            if (offset.HasValue) sql += " OFFSET @offset";
        }
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        if (limit.HasValue) cmd.Parameters.AddWithValue("@limit", limit.Value);
        if (offset.HasValue) cmd.Parameters.AddWithValue("@offset", offset.Value);
        using var reader = cmd.ExecuteReader();
        return ReadRecords(reader);
    }

    public CardRecord? GetById(object id, string idColumn = "face_id")
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT * FROM '{_tableName}' WHERE {idColumn} = @id LIMIT 1;";
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = cmd.ExecuteReader();
        return ReadRecords(reader).FirstOrDefault();
    }

    public IReadOnlyList<CardRecord> GetByName(string name, string nameColumn = "cardname")
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT * FROM '{_tableName}' WHERE {nameColumn} = @name;";
        cmd.Parameters.AddWithValue("@name", name);
        using var reader = cmd.ExecuteReader();
        return ReadRecords(reader);
    }

    public IReadOnlyList<CardRecord> Search(string whereClause, IReadOnlyDictionary<string, object?>? parameters = null)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT * FROM '{_tableName}' WHERE {whereClause};";
        if (parameters is not null)
        {
            foreach (var kv in parameters)
            {
                cmd.Parameters.AddWithValue(kv.Key.StartsWith("@") ? kv.Key : "@" + kv.Key, kv.Value ?? DBNull.Value);
            }
        }
        using var reader = cmd.ExecuteReader();
        return ReadRecords(reader);
    }

    private static List<CardRecord> ReadRecords(SqliteDataReader reader)
    {
        var list = new List<CardRecord>();
        while (reader.Read())
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                object? val = reader.IsDBNull(i) ? null : reader.GetValue(i);
                dict[name] = val;
            }
            list.Add(new CardRecord { Data = dict });
        }
        return list;
    }
}
