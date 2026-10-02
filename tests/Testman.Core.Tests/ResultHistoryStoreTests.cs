using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Testman.Core.Persistence;

namespace Testman.Core.Tests;

public sealed class ResultHistoryStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"testman-results-{Guid.NewGuid():N}");
    private readonly string databasePath;
    private readonly string specificationPath;

    public ResultHistoryStoreTests()
    {
        Directory.CreateDirectory(directory);
        databasePath = Path.Combine(directory, "testman.db");
        specificationPath = Path.Combine(directory, "specs", "example.md");
        Directory.CreateDirectory(Path.GetDirectoryName(specificationPath)!);
        File.WriteAllText(specificationPath, "# Example");
        Git("init");
        Git("config", "user.name", "Test User");
        Git("config", "user.email", "test@example.invalid");
        Git("add", "specs/example.md");
        Git("commit", "-m", "Add specification");
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
    public void ReadLatest_uses_the_largest_result_id_not_the_execution_time()
    {
        var store = new ResultHistoryStore(databasePath);
        var specification = GitSpecificationReference.Resolve(specificationPath);
        store.Append(new ResultSubmission(
            new DateTimeOffset(2026, 10, 2, 12, 0, 0, TimeSpan.Zero),
            "First tester",
            [new TestResultInput(specification, "TC-1", TestResultOutcome.Fail, "first")]));
        store.Append(new ResultSubmission(
            new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero),
            "Latest tester",
            [new TestResultInput(specification, "TC-1", TestResultOutcome.Pass, "latest")]));

        var latest = store.ReadLatest(specification, "TC-1");

        Assert.NotNull(latest);
        Assert.Equal(TestResultOutcome.Pass, latest.Outcome);
        Assert.Equal("latest", latest.Comment);
        Assert.Equal("Latest tester", latest.ExecutedBy);
        Assert.Equal(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero), latest.ExecutedAtUtc);
    }

    [Fact]
    public void ReadLatest_returns_null_when_the_test_case_has_no_history()
    {
        var store = new ResultHistoryStore(databasePath);
        var specification = GitSpecificationReference.Resolve(specificationPath);
        store.Append(new ResultSubmission(
            DateTimeOffset.UtcNow,
            "Tester",
            [new TestResultInput(specification, "TC-1", TestResultOutcome.Pass, null)]));

        Assert.Null(store.ReadLatest(specification, "TC-2"));
    }

    [Fact]
    public void ReadHistory_returns_all_results_newest_first()
    {
        var store = new ResultHistoryStore(databasePath);
        var specification = GitSpecificationReference.Resolve(specificationPath);
        store.Append(new ResultSubmission(
            new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
            "First",
            [new TestResultInput(specification, "TC-1", TestResultOutcome.Blocked, null)]));
        store.Append(new ResultSubmission(
            new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero),
            "Second",
            [new TestResultInput(specification, "TC-1", TestResultOutcome.NotApplicable, "later")]));

        var history = store.ReadHistory(specification, "TC-1");

        Assert.Collection(
            history,
            latest =>
            {
                Assert.Equal(TestResultOutcome.NotApplicable, latest.Outcome);
                Assert.Equal("Second", latest.ExecutedBy);
                Assert.Equal("later", latest.Comment);
            },
            first =>
            {
                Assert.Equal(TestResultOutcome.Blocked, first.Outcome);
                Assert.Equal("First", first.ExecutedBy);
                Assert.Null(first.Comment);
            });
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
    public void Append_rolls_back_the_submission_when_a_later_database_insert_fails()
    {
        using (var connection = SqliteConnectionFactory.Open(databasePath, pooling: false))
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TRIGGER reject_second_result
                BEFORE INSERT ON test_results
                WHEN NEW.test_case_id = 'TC-2'
                BEGIN
                    SELECT RAISE(ABORT, 'simulated failure');
                END;
                """;
            command.ExecuteNonQuery();
        }

        var store = new ResultHistoryStore(databasePath);

        Assert.Throws<SqliteException>(() => store.Append(new ResultSubmission(
            DateTimeOffset.UtcNow,
            "Tester",
            [Result("TC-1", TestResultOutcome.Pass, null), Result("TC-2", TestResultOutcome.Fail, null)])));

        using var verified = SqliteConnectionFactory.Open(databasePath, pooling: false);
        Assert.Equal(0L, ExecuteScalar<long>(verified, "SELECT COUNT(*) FROM result_submissions"));
        Assert.Equal(0L, ExecuteScalar<long>(verified, "SELECT COUNT(*) FROM test_results"));
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

    public void Dispose() => DeleteDirectory(directory);

    private TestResultInput Result(string testCaseId, TestResultOutcome outcome, string? comment) =>
        new(GitSpecificationReference.Resolve(specificationPath), testCaseId, outcome, comment);

    private void Git(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = directory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo)!;
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, error);
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

    private static void DeleteDirectory(string path)
    {
        foreach (var entry in new DirectoryInfo(path).EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
        {
            entry.Attributes = FileAttributes.Normal;
        }

        Directory.Delete(path, recursive: true);
    }
}
