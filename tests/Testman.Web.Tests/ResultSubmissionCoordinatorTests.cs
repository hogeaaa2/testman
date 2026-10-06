using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Testman.Core.Persistence;
using Testman.Web.Presentation;

namespace Testman.Web.Tests;

public sealed class ResultSubmissionCoordinatorTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"testman-submit-{Guid.NewGuid():N}");
    private readonly string specificationPath;
    private readonly string databasePath;
    private readonly ResultSubmissionCoordinator coordinator;

    public ResultSubmissionCoordinatorTests()
    {
        Directory.CreateDirectory(directory);
        specificationPath = Path.Combine(directory, "spec.md");
        databasePath = Path.Combine(directory, "results.db");
        File.WriteAllText(specificationPath, Specification());
        Git("init");
        Git("config", "user.name", "Test User");
        Git("config", "user.email", "test@example.invalid");
        Git("add", "spec.md");
        Git("commit", "-m", "Add specification");
        DatabaseMigrationRunner.Apply(databasePath);
        coordinator = new ResultSubmissionCoordinator(
            specificationPath,
            directory,
            new ResultHistoryStore(databasePath),
            new FixedTimeProvider(new DateTimeOffset(2026, 10, 2, 3, 4, 5, TimeSpan.Zero)));
    }

    [Fact]
    public void Submit_saves_all_selected_current_cases_together()
    {
        var result = coordinator.Submit(new ResultSubmissionRequest(
            " Tester ",
            " app.exe ",
            [Input("TC-1", "pass"), Input("TC-2", "blocked")],
            ConfirmPartial: false));

        Assert.Equal(ResultSubmissionStatus.Saved, result.Status);
        using var connection = SqliteConnectionFactory.Open(databasePath, pooling: false);
        Assert.Equal(1L, Scalar(connection, "SELECT COUNT(*) FROM result_submissions"));
        Assert.Equal("app.exe", TextScalar(connection, "SELECT test_target_name FROM result_submissions"));
        Assert.Equal(2L, Scalar(connection, "SELECT COUNT(*) FROM test_results"));
    }

    [Fact]
    public void Submit_requires_confirmation_before_saving_a_partial_selection()
    {
        var request = new ResultSubmissionRequest(
            "Tester",
            "app.exe",
            [Input("TC-1", "fail"), Input("TC-2", null)],
            ConfirmPartial: false);

        var confirmation = coordinator.Submit(request);
        Assert.Equal(ResultSubmissionStatus.NeedsPartialConfirmation, confirmation.Status);
        Assert.Equal(0L, CountResults());

        var saved = coordinator.Submit(request with { ConfirmPartial = true });
        Assert.Equal(ResultSubmissionStatus.Saved, saved.Status);
        Assert.Equal(1L, CountResults());
    }

    [Fact]
    public void Saved_results_remain_distinct_when_page_content_refreshes_the_git_revision()
    {
        var saved = coordinator.Submit(new ResultSubmissionRequest(
            "Tester",
            "app.exe",
            [Input("TC-1", "pass"), Input("TC-2", "blocked")],
            ConfirmPartial: false));
        Assert.Equal(ResultSubmissionStatus.Saved, saved.Status);
        var source = new SpecificationPageContentSource(
            specificationPath, directory, new ResultHistoryStore(databasePath));

        var firstFile = Assert.Single(source.Load().Files);
        var firstCases = Assert.Single(firstFile.Titles).VerificationCases;
        Assert.Equal(TestResultOutcome.Pass, firstCases[0].PreviousResult?.Outcome);
        Assert.Equal(TestResultOutcome.Blocked, firstCases[1].PreviousResult?.Outcome);

        File.AppendAllText(specificationPath, "\n");
        Git("add", "spec.md");
        Git("commit", "-m", "Update specification");

        var refreshedFile = Assert.Single(source.Load().Files);
        Assert.NotEqual(firstFile.SpecificationRevision, refreshedFile.SpecificationRevision);
        Assert.Equal(
            GitSpecificationReference.ResolveLastCommittedRevision(specificationPath),
            refreshedFile.SpecificationRevision);
        var refreshedCases = Assert.Single(refreshedFile.Titles).VerificationCases;
        Assert.Equal(["TC-1", "TC-2"], refreshedCases.Select(testCase => testCase.Id));
        Assert.Equal(TestResultOutcome.Pass, refreshedCases[0].PreviousResult?.Outcome);
        Assert.Equal(TestResultOutcome.Blocked, refreshedCases[1].PreviousResult?.Outcome);
        Assert.All(refreshedCases, testCase =>
            Assert.Equal(firstFile.SpecificationRevision, Assert.Single(testCase.History).SpecificationRevision));
    }

    [Fact]
    public void Page_content_rechecks_git_identity_on_each_load_and_recovers_after_failure()
    {
        var saved = coordinator.Submit(new ResultSubmissionRequest(
            "Tester",
            "app.exe",
            [Input("TC-1", "pass"), Input("TC-2", "blocked")],
            ConfirmPartial: false));
        Assert.Equal(ResultSubmissionStatus.Saved, saved.Status);
        var source = new SpecificationPageContentSource(
            specificationPath, directory, new ResultHistoryStore(databasePath));
        var firstFile = Assert.Single(source.Load().Files);
        Assert.NotNull(firstFile.SpecificationRevision);

        var gitDirectory = Path.Combine(directory, ".git");
        var hiddenGitDirectory = Path.Combine(directory, ".git-hidden");
        Directory.Move(gitDirectory, hiddenGitDirectory);
        try
        {
            var unavailableFile = Assert.Single(source.Load().Files);
            Assert.Null(unavailableFile.SpecificationRevision);
            Assert.All(Assert.Single(unavailableFile.Titles).VerificationCases, testCase =>
            {
                Assert.Null(testCase.PreviousResult);
                Assert.Empty(testCase.History);
            });
        }
        finally
        {
            Directory.Move(hiddenGitDirectory, gitDirectory);
        }

        var recoveredFile = Assert.Single(source.Load().Files);
        Assert.Equal(firstFile.SpecificationRevision, recoveredFile.SpecificationRevision);
        Assert.All(Assert.Single(recoveredFile.Titles).VerificationCases, testCase =>
            Assert.Single(testCase.History));
    }

    [Fact]
    public void Submit_rejects_no_selection_and_tampered_case_identity()
    {
        var empty = coordinator.Submit(new ResultSubmissionRequest(
            "Tester",
            "app.exe",
            [Input("TC-1", null), Input("TC-2", null)],
            ConfirmPartial: false));
        var tampered = coordinator.Submit(new ResultSubmissionRequest(
            "Tester",
            "app.exe",
            [Input("TC-1", "pass"), Input("TC-999", "fail")],
            ConfirmPartial: true));
        var malformedPath = coordinator.Submit(new ResultSubmissionRequest(
            "Tester",
            "app.exe",
            [new ResultCaseInput("\0", "TC-1", "pass", null), Input("TC-2", "pass")],
            ConfirmPartial: true));
        var wrongCase = coordinator.Submit(new ResultSubmissionRequest(
            "Tester",
            "app.exe",
            [Input("tc-1", "pass"), Input("TC-2", "pass")],
            ConfirmPartial: true));

        Assert.Equal(ResultSubmissionStatus.Invalid, empty.Status);
        Assert.Equal(ResultSubmissionStatus.Invalid, tampered.Status);
        Assert.Equal(ResultSubmissionStatus.Invalid, malformedPath.Status);
        Assert.Equal(ResultSubmissionStatus.Invalid, wrongCase.Status);
        Assert.Equal(0L, CountResults());
    }

    [Fact]
    public void Submit_requires_a_test_target_name()
    {
        var result = coordinator.Submit(new ResultSubmissionRequest(
            "Tester",
            "  ",
            [Input("TC-1", "pass"), Input("TC-2", "pass")],
            ConfirmPartial: false));

        Assert.Equal(ResultSubmissionStatus.Invalid, result.Status);
        Assert.Equal("Test target name is required.", result.Message);
        Assert.Equal(0L, CountResults());
    }

    [Fact]
    public void Submit_rejects_a_specification_with_uncommitted_changes()
    {
        File.AppendAllText(specificationPath, "\nchanged");

        var result = coordinator.Submit(new ResultSubmissionRequest(
            "Tester",
            "app.exe",
            [Input("TC-1", "pass"), Input("TC-2", "pass")],
            ConfirmPartial: false));

        Assert.Equal(ResultSubmissionStatus.Invalid, result.Status);
        Assert.Equal(0L, CountResults());
    }

    public void Dispose()
    {
        foreach (var entry in new DirectoryInfo(directory).EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
            entry.Attributes = FileAttributes.Normal;
        Directory.Delete(directory, recursive: true);
    }

    private ResultCaseInput Input(string id, string? outcome) => new(specificationPath, id, outcome, $"comment {id}");

    private long CountResults()
    {
        using var connection = SqliteConnectionFactory.Open(databasePath, pooling: false);
        return Scalar(connection, "SELECT COUNT(*) FROM test_results");
    }

    private static long Scalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (long)command.ExecuteScalar()!;
    }

    private static string TextScalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (string)command.ExecuteScalar()!;
    }

    private void Git(params string[] arguments)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = directory, UseShellExecute = false, RedirectStandardError = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, error);
    }

    private static string Specification() => """
        Testman-Format-Version: 1

        # Submit

        ## Overview

        Overview

        ## Preconditions

        None

        ## Common steps

        None

        | ID | Major item | Middle item | Minor item | Steps | Expected result |
        |---|---|---|---|---|---|
        | TC-1 | A | B | C | - | One |
        | TC-2 | A | B | D | - | Two |
        """;

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
