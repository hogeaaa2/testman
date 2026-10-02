using System.Globalization;
using Microsoft.Data.Sqlite;
using Testman.Core.Specifications;

namespace Testman.Core.Persistence;

public enum TestResultOutcome
{
    Pass,
    Fail,
    Blocked,
    NotApplicable,
}

public sealed record TestResultInput(
    GitSpecificationReference Specification,
    string TestCaseId,
    TestResultOutcome Outcome,
    string? Comment);

public sealed record ResultSubmission(
    DateTimeOffset ExecutedAt,
    string ExecutedBy,
    IReadOnlyList<TestResultInput> Results);

public sealed record TestResultRecord(
    long Id,
    long SubmissionId,
    DateTimeOffset ExecutedAtUtc,
    string ExecutedBy,
    string TestCaseId,
    TestResultOutcome Outcome,
    string? Comment,
    string SpecificationRevision);

public sealed class ResultHistoryStore(string databasePath)
{
    private readonly string databasePath = Path.GetFullPath(
        string.IsNullOrWhiteSpace(databasePath)
            ? throw new ArgumentException("Database path is required.", nameof(databasePath))
            : databasePath);

    public long Append(ResultSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(submission);

        var executedBy = submission.ExecutedBy?.Trim();
        if (string.IsNullOrEmpty(executedBy))
        {
            throw new ArgumentException("Executor name is required.", nameof(submission));
        }

        if (submission.Results is null || submission.Results.Count == 0)
        {
            throw new ArgumentException("At least one result is required.", nameof(submission));
        }

        var results = submission.Results.Select(Validate).ToArray();

        using var connection = SqliteConnectionFactory.Open(databasePath, pooling: false);
        using var transaction = connection.BeginTransaction();

        var submissionId = InsertSubmission(connection, transaction, submission.ExecutedAt, executedBy);
        foreach (var result in results)
        {
            InsertResult(connection, transaction, submissionId, result);
        }

        transaction.Commit();
        return submissionId;
    }

    public TestResultRecord? ReadLatest(
        GitSpecificationReference specification,
        string testCaseId)
    {
        return Read(specification, testCaseId, latestOnly: true).SingleOrDefault();
    }

    public IReadOnlyList<TestResultRecord> ReadHistory(
        GitSpecificationReference specification,
        string testCaseId)
    {
        return Read(specification, testCaseId, latestOnly: false);
    }

    private IReadOnlyList<TestResultRecord> Read(
        GitSpecificationReference specification,
        string testCaseId,
        bool latestOnly)
    {
        ArgumentNullException.ThrowIfNull(specification);
        if (!TestId.TryParse(testCaseId, out var parsedTestId))
        {
            throw new ArgumentException($"Invalid Test ID: {testCaseId}", nameof(testCaseId));
        }

        using var connection = SqliteConnectionFactory.Open(databasePath, pooling: false);
        using var command = connection.CreateCommand();
        command.CommandText = $$"""
            SELECT
                result.id,
                result.submission_id,
                submission.executed_at_utc,
                submission.executed_by,
                result.test_case_id,
                result.result,
                result.comment,
                result.specification_revision
            FROM test_results AS result
            INNER JOIN result_submissions AS submission ON submission.id = result.submission_id
            WHERE result.repository_root = $repositoryRoot
              AND result.source_file = $sourceFile
              AND result.test_case_id = $testCaseId
            ORDER BY result.id DESC
            {{(latestOnly ? "LIMIT 1" : string.Empty)}}
            """;
        command.Parameters.AddWithValue("$repositoryRoot", specification.RepositoryRoot);
        command.Parameters.AddWithValue("$sourceFile", specification.SourceFile);
        command.Parameters.AddWithValue("$testCaseId", parsedTestId.Value);

        using var reader = command.ExecuteReader();
        var records = new List<TestResultRecord>();
        while (reader.Read())
        {
            records.Add(new TestResultRecord(
                reader.GetInt64(0),
                reader.GetInt64(1),
                DateTimeOffset.Parse(
                    reader.GetString(2),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
                reader.GetString(3),
                reader.GetString(4),
                FromDatabaseValue(reader.GetString(5)),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.GetString(7)));
        }

        return records;
    }

    private static ValidatedResult Validate(TestResultInput result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!TestId.TryParse(result.TestCaseId, out var testId))
        {
            throw new ArgumentException($"Invalid Test ID: {result.TestCaseId}", nameof(result));
        }

        if (!Enum.IsDefined(result.Outcome))
        {
            throw new ArgumentException("Invalid test result outcome.", nameof(result));
        }

        ArgumentNullException.ThrowIfNull(result.Specification);

        return new ValidatedResult(
            result.Specification.RepositoryRoot,
            result.Specification.SourceFile,
            testId.Value,
            ToDatabaseValue(result.Outcome),
            string.IsNullOrWhiteSpace(result.Comment) ? null : result.Comment,
            result.Specification.SpecificationRevision);
    }

    private static long InsertSubmission(
        SqliteConnection connection,
        SqliteTransaction transaction,
        DateTimeOffset executedAt,
        string executedBy)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO result_submissions(executed_at_utc, executed_by)
            VALUES ($executedAtUtc, $executedBy);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue(
            "$executedAtUtc",
            executedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$executedBy", executedBy);
        return (long)command.ExecuteScalar()!;
    }

    private static void InsertResult(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long submissionId,
        ValidatedResult result)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO test_results(
                submission_id,
                repository_root,
                source_file,
                test_case_id,
                result,
                comment,
                specification_revision)
            VALUES (
                $submissionId,
                $repositoryRoot,
                $sourceFile,
                $testCaseId,
                $result,
                $comment,
                $specificationRevision)
            """;
        command.Parameters.AddWithValue("$submissionId", submissionId);
        command.Parameters.AddWithValue("$repositoryRoot", result.RepositoryRoot);
        command.Parameters.AddWithValue("$sourceFile", result.SourceFile);
        command.Parameters.AddWithValue("$testCaseId", result.TestCaseId);
        command.Parameters.AddWithValue("$result", result.Outcome);
        command.Parameters.AddWithValue("$comment", (object?)result.Comment ?? DBNull.Value);
        command.Parameters.AddWithValue("$specificationRevision", result.SpecificationRevision);
        command.ExecuteNonQuery();
    }

    private static string ToDatabaseValue(TestResultOutcome outcome) => outcome switch
    {
        TestResultOutcome.Pass => "pass",
        TestResultOutcome.Fail => "fail",
        TestResultOutcome.Blocked => "blocked",
        TestResultOutcome.NotApplicable => "not_applicable",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
    };

    private static TestResultOutcome FromDatabaseValue(string outcome) => outcome switch
    {
        "pass" => TestResultOutcome.Pass,
        "fail" => TestResultOutcome.Fail,
        "blocked" => TestResultOutcome.Blocked,
        "not_applicable" => TestResultOutcome.NotApplicable,
        _ => throw new InvalidDataException($"Unknown stored test result: {outcome}"),
    };

    private sealed record ValidatedResult(
        string RepositoryRoot,
        string SourceFile,
        string TestCaseId,
        string Outcome,
        string? Comment,
        string SpecificationRevision);
}
