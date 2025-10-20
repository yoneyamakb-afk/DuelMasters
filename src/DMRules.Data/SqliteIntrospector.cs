using Microsoft.Data.Sqlite;

namespace DMRules.Data;

public sealed class TableSchema
{
    public required string Name { get; init; }
    public required List<ColumnSchema> Columns { get; init; } = new();
}

public sealed class ColumnSchema
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public bool IsPrimaryKey { get; init; }
    public bool IsNotNull { get; init; }
    public string? DefaultValue { get; init; }
}

public static class SqliteIntrospector
{
    public static IReadOnlyList<TableSchema> ReadSchema(string dbPath)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name, type FROM sqlite_master WHERE type IN ('table','view') ORDER BY type, name;";
        using var reader = cmd.ExecuteReader();

        var tables = new List<string>();
        while (reader.Read())
        {
            var name = reader.GetString(0);
            var type = reader.GetString(1);
            if (type == "table") tables.Add(name);
        }

        var result = new List<TableSchema>();
        foreach (var t in tables)
        {
            var c = conn.CreateCommand();
            c.CommandText = $"PRAGMA table_info('{t}')";
            using var r = c.ExecuteReader();
            var cols = new List<ColumnSchema>();
            while (r.Read())
            {
                cols.Add(new ColumnSchema
                {
                    Name = r.GetString(1),
                    Type = r.GetString(2),
                    IsNotNull = r.GetInt32(3) == 1,
                    DefaultValue = r.IsDBNull(4) ? null : r.GetString(4),
                    IsPrimaryKey = r.GetInt32(5) == 1,
                });
            }
            result.Add(new TableSchema { Name = t, Columns = cols });
        }
        return result;
    }

    public static bool TableExists(string dbPath, string table)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name=@name";
        cmd.Parameters.AddWithValue("@name", table);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public static bool ColumnExists(string dbPath, string table, string column)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info('{table}')";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            if (string.Equals(r.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
