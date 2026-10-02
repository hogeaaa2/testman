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
    string RepositoryRoot,
    string SourceFile,
    string TestCaseId,
    TestResultOutcome Outcome,
    string? Comment,
    string SpecificationRevision);

public sealed record ResultSubmission(
    DateTimeOffset ExecutedAt,
    string ExecutedBy,
    IReadOnlyList<TestResultInput> Results);

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

        if (string.IsNullOrWhiteSpace(result.RepositoryRoot))
        {
            throw new ArgumentException("Repository root is required.", nameof(result));
        }

        if (string.IsNullOrWhiteSpace(result.SourceFile) || Path.IsPathRooted(result.SourceFile))
        {
            throw new ArgumentException("Source file must be a relative path.", nameof(result));
        }

        if (string.IsNullOrWhiteSpace(result.SpecificationRevision))
        {
            throw new ArgumentException("Specification revision is required.", nameof(result));
        }

        return new ValidatedResult(
            Path.GetFullPath(result.RepositoryRoot),
            result.SourceFile.Replace('\\', '/'),
            testId.Value,
            ToDatabaseValue(result.Outcome),
            string.IsNullOrWhiteSpace(result.Comment) ? null : result.Comment,
            result.SpecificationRevision);
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

    private sealed record ValidatedResult(
        string RepositoryRoot,
        string SourceFile,
        string TestCaseId,
        string Outcome,
        string? Comment,
        string SpecificationRevision);
}
