using Microsoft.Data.Sqlite;
using Testman.Core.Persistence;

namespace Testman.Core.Tests;

public sealed class ResultHistoryStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"testman-results-{Guid.NewGuid():N}");
    private readonly string databasePath;

    public ResultHistoryStoreTests()
    {
        Directory.CreateDirectory(directory);
        databasePath = Path.Combine(directory, "testman.db");
        DatabaseMigrationRunner.Apply(databasePath);
    }

    [Fact]
    public void Append_saves_selected_results_as_one_trimmed_submission()
    {
        var store = new ResultHistoryStore(databasePath);

        var submissionId = store.Append(new ResultSubmission(
            new DateTimeOffset(2026, 10, 2, 12, 34, 56, TimeSpan.FromHours(9)),
            "  Tester  ",
            [
                Result("TC-1", TestResultOutcome.Pass, "works"),
                Result("TC-2", TestResultOutcome.NotApplicable, "  "),
            ]));

        using var connection = SqliteConnectionFactory.Open(databasePath, pooling: false);
        Assert.Equal(
            [$"{submissionId}|2026-10-02T03:34:56.0000000Z|Tester"],
            ReadStrings(connection, "SELECT id || '|' || executed_at_utc || '|' || executed_by FROM result_submissions"));
        Assert.Equal(
            [
                $"{submissionId}|TC-1|pass|works",
                $"{submissionId}|TC-2|not_applicable|<null>",
            ],
            ReadStrings(connection, "SELECT submission_id || '|' || test_case_id || '|' || result || '|' || COALESCE(comment, '<null>') FROM test_results ORDER BY id"));
    }

    [Fact]
    public void Append_preserves_previous_results_for_the_same_test_case()
    {
        var store = new ResultHistoryStore(databasePath);
        store.Append(new ResultSubmission(DateTimeOffset.UtcNow, "Tester", [Result("TC-1", TestResultOutcome.Fail, null)]));
        store.Append(new ResultSubmission(DateTimeOffset.UtcNow, "Tester", [Result("TC-1", TestResultOutcome.Pass, null)]));

        using var connection = SqliteConnectionFactory.Open(databasePath, pooling: false);
        Assert.Equal(
            ["fail", "pass"],
            ReadStrings(connection, "SELECT result FROM test_results ORDER BY id"));
    }

    [Fact]
    public void Append_rejects_an_invalid_batch_without_saving_any_part_of_it()
    {
        var store = new ResultHistoryStore(databasePath);
        var invalid = Result("not-an-id", TestResultOutcome.Fail, null);

        Assert.Throws<ArgumentException>(() => store.Append(new ResultSubmission(
            DateTimeOffset.UtcNow,
            "Tester",
            [Result("TC-1", TestResultOutcome.Pass, null), invalid])));

        using var connection = SqliteConnectionFactory.Open(databasePath, pooling: false);
        Assert.Equal(0L, ExecuteScalar<long>(connection, "SELECT COUNT(*) FROM result_submissions"));
        Assert.Equal(0L, ExecuteScalar<long>(connection, "SELECT COUNT(*) FROM test_results"));
    }

    [Fact]
    public void Append_requires_an_executor_and_at_least_one_result()
    {
        var store = new ResultHistoryStore(databasePath);

        Assert.Throws<ArgumentException>(() => store.Append(
            new ResultSubmission(DateTimeOffset.UtcNow, "  ", [Result("TC-1", TestResultOutcome.Pass, null)])));
        Assert.Throws<ArgumentException>(() => store.Append(
            new ResultSubmission(DateTimeOffset.UtcNow, "Tester", [])));
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);

    private static TestResultInput Result(string testCaseId, TestResultOutcome outcome, string? comment) =>
        new("D:/repo", "specs/example.md", testCaseId, outcome, comment, "0123456789abcdef");

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
}
