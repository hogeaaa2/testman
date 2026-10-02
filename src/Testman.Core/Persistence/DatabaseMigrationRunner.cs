using System.Globalization;
using System.Reflection;
using Microsoft.Data.Sqlite;

namespace Testman.Core.Persistence;

public static class DatabaseMigrationRunner
{
    private static readonly Migration[] Migrations =
    [
        new(1, "create_result_history", "001_create_result_history.sql"),
    ];

    public static void Apply(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var fullPath = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        using var connection = SqliteConnectionFactory.Open(fullPath, pooling: false);

        var appliedVersions = ReadAppliedVersions(connection);
        foreach (var migration in Migrations.Where(item => !appliedVersions.Contains(item.Version)))
        {
            using var transaction = connection.BeginTransaction();
            Execute(connection, ReadMigrationSql(migration.ResourceSuffix), transaction);

            using var record = connection.CreateCommand();
            record.Transaction = transaction;
            record.CommandText = "INSERT INTO schema_migrations(version, name, applied_at_utc) VALUES ($version, $name, $appliedAtUtc)";
            record.Parameters.AddWithValue("$version", migration.Version);
            record.Parameters.AddWithValue("$name", migration.Name);
            record.Parameters.AddWithValue(
                "$appliedAtUtc",
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            record.ExecuteNonQuery();
            transaction.Commit();
        }
    }

    private static HashSet<int> ReadAppliedVersions(SqliteConnection connection)
    {
        using var tableExists = connection.CreateCommand();
        tableExists.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'schema_migrations'";
        if ((long)tableExists.ExecuteScalar()! == 0)
        {
            return [];
        }

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT version FROM schema_migrations";
        using var reader = command.ExecuteReader();
        var versions = new HashSet<int>();
        while (reader.Read())
        {
            versions.Add(reader.GetInt32(0));
        }

        return versions;
    }

    private static string ReadMigrationSql(string resourceSuffix)
    {
        var assembly = typeof(DatabaseMigrationRunner).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith(resourceSuffix, StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static void Execute(
        SqliteConnection connection,
        string sql,
        SqliteTransaction? transaction = null)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private sealed record Migration(int Version, string Name, string ResourceSuffix);
}
