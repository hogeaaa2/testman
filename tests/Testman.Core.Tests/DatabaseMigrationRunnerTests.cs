using Microsoft.Data.Sqlite;
using Testman.Core.Persistence;

namespace Testman.Core.Tests;

public sealed class DatabaseMigrationRunnerTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"testman-db-{Guid.NewGuid():N}");
    private readonly string databasePath;

    public DatabaseMigrationRunnerTests()
    {
        Directory.CreateDirectory(directory);
        databasePath = Path.Combine(directory, "testman.db");
    }

    [Fact]
    public void Apply_creates_the_result_history_schema_and_records_the_migration()
    {
        DatabaseMigrationRunner.Apply(databasePath);

        using var connection = OpenConnection();
        Assert.Equal(
            ["result_submissions", "schema_migrations", "test_results"],
            ReadStrings(connection, "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name"));
        Assert.Equal(["1:create_result_history"],
            ReadStrings(connection, "SELECT CAST(version AS TEXT) || ':' || name FROM schema_migrations ORDER BY version"));
        Assert.Equal(2, ExecuteScalar<long>(connection, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name LIKE 'ix_test_results_%'"));
        Assert.Equal(
            ["id", "executed_at_utc", "executed_by", "test_target_name"],
            ReadStrings(connection, "SELECT name FROM pragma_table_info('result_submissions') ORDER BY cid"));
    }

    [Fact]
    public void Apply_is_idempotent_and_preserves_existing_history()
    {
        DatabaseMigrationRunner.Apply(databasePath);
        using (var connection = OpenConnection())
        {
            Execute(connection, "INSERT INTO result_submissions(executed_at_utc, executed_by, test_target_name) VALUES ('2026-10-02T00:00:00.0000000Z', 'Tester', 'app.exe')");
            Execute(connection, "INSERT INTO test_results(submission_id, repository_root, source_file, test_case_id, result, specification_revision) VALUES (1, 'D:/repo', 'spec.md', 'TC-1', 'pass', 'abc123')");
        }

        DatabaseMigrationRunner.Apply(databasePath);

        using var verified = OpenConnection();
        Assert.Equal(1, ExecuteScalar<long>(verified, "SELECT COUNT(*) FROM schema_migrations"));
        Assert.Equal(1, ExecuteScalar<long>(verified, "SELECT COUNT(*) FROM test_results"));
    }

    [Fact]
    public void Opened_connections_enforce_the_submission_foreign_key_without_cascade_delete()
    {
        DatabaseMigrationRunner.Apply(databasePath);
        using var connection = OpenConnection();

        Assert.Throws<SqliteException>(() => Execute(
            connection,
            "INSERT INTO test_results(submission_id, repository_root, source_file, test_case_id, result, specification_revision) VALUES (999, 'D:/repo', 'spec.md', 'TC-1', 'pass', 'abc123')"));

        Execute(connection, "INSERT INTO result_submissions(executed_at_utc, executed_by, test_target_name) VALUES ('2026-10-02T00:00:00.0000000Z', 'Tester', 'app.exe')");
        Execute(connection, "INSERT INTO test_results(submission_id, repository_root, source_file, test_case_id, result, specification_revision) VALUES (1, 'D:/repo', 'spec.md', 'TC-1', 'pass', 'abc123')");

        Assert.Throws<SqliteException>(() => Execute(connection, "DELETE FROM result_submissions WHERE id = 1"));
        Assert.Equal(1, ExecuteScalar<long>(connection, "SELECT COUNT(*) FROM test_results"));
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);

    private SqliteConnection OpenConnection()
    {
        return SqliteConnectionFactory.Open(databasePath, pooling: false);
    }

    private static IReadOnlyList<string> ReadStrings(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = command.ExecuteReader();
        var values = new List<string>();
        while (reader.Read()) values.Add(reader.GetString(0));
        return values;
    }

    private static T ExecuteScalar<T>(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (T)Convert.ChangeType(command.ExecuteScalar()!, typeof(T));
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
