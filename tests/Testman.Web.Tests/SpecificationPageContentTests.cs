using Testman.Core.Persistence;
using Testman.Core.Specifications;
using Testman.Web.Presentation;

namespace Testman.Web.Tests;

public sealed class SpecificationPageContentTests
{
    [Fact]
    public void Create_keeps_valid_titles_when_another_file_has_diagnostics()
    {
        var valid = Parse("valid.md", ValidSpecification("Valid title", "<strong>Overview</strong>"));
        var invalid = Parse("invalid.md", "Testman-Format-Version: 1\n\n# Invalid title");
        var catalog = new SpecificationCatalogResult(
            [valid, invalid],
            valid.Specification.Diagnostics.Concat(invalid.Specification.Diagnostics).ToList());

        var content = SpecificationPageContent.Create(catalog);

        var file = Assert.Single(content.Files);
        Assert.Equal("valid.md", file.SourcePath);
        Assert.Equal(1, file.FormatVersion);
        Assert.Equal("Valid title", Assert.Single(file.Titles).Name);
        var diagnostic = Assert.Single(content.Diagnostics);
        Assert.Equal("invalid.md", diagnostic.SourcePath);
        Assert.Equal(3, diagnostic.LineNumber);
        Assert.Contains("Required section", diagnostic.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_sanitizes_overview_html_and_exposes_pattern_classification_without_test_ids()
    {
        var file = Parse(
            "unsafe.md",
            ValidSpecification("Unsafe", "Overview<script>alert('x')</script>"));
        var catalog = new SpecificationCatalogResult([file], file.Specification.Diagnostics);

        var content = SpecificationPageContent.Create(catalog);

        var title = Assert.Single(Assert.Single(content.Files).Titles);
        Assert.Contains("Overview", title.OverviewHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("script", title.OverviewHtml, StringComparison.OrdinalIgnoreCase);
        var pattern = Assert.Single(title.Patterns);
        Assert.Equal("Major", pattern.MajorItem);
        Assert.Equal("Middle", pattern.MiddleItem);
        Assert.Equal("Minor", pattern.MinorItem);
        Assert.DoesNotContain("TC-1", title.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Create_prepares_sanitized_verification_details()
    {
        var source = ValidSpecification("Verification", "Overview")
            .Replace("None\n\n## Common steps", "Precondition <script>unsafe()</script>\n\n## Common steps", StringComparison.Ordinal)
            .Replace("None\n\n| ID", "Common **step**\n\n| ID", StringComparison.Ordinal)
            .Replace("| TC-1 | Major | Middle | Minor | - | Success |", "| TC-1 | Major | Middle | Minor | Click **save** | Shows <em>success</em> |", StringComparison.Ordinal);
        var file = Parse("verification.md", source);

        var content = SpecificationPageContent.Create(
            new SpecificationCatalogResult([file], file.Specification.Diagnostics));

        var title = Assert.Single(Assert.Single(content.Files).Titles);
        Assert.Contains("Precondition", title.PreconditionsHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("script", title.PreconditionsHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<strong>step</strong>", title.CommonStepsHtml, StringComparison.Ordinal);
        var testCase = Assert.Single(title.VerificationCases);
        Assert.Equal("TC-1", testCase.Id);
        Assert.Contains("<strong>save</strong>", testCase.StepsHtml, StringComparison.Ordinal);
        Assert.Contains("<em>success</em>", testCase.ExpectedResultHtml, StringComparison.Ordinal);
        Assert.True(testCase.CanRegisterResult);
    }

    [Fact]
    public void Create_adds_the_latest_result_and_history_to_verification_cases()
    {
        var file = Parse("verification.md", ValidSpecification("Verification", "Overview"));
        var executedAtUtc = new DateTimeOffset(2026, 10, 2, 3, 4, 5, TimeSpan.Zero);
        var history = new[]
        {
            new TestResultRecord(2, 2, executedAtUtc, "Latest", "TC-1", TestResultOutcome.Pass, "done", "def456"),
            new TestResultRecord(1, 1, executedAtUtc.AddDays(-1), "First", "TC-1", TestResultOutcome.Fail, null, "abc123"),
        };

        var content = SpecificationPageContent.Create(
            new SpecificationCatalogResult([file], file.Specification.Diagnostics),
            (sourcePath, testCaseId) =>
            {
                Assert.Equal("verification.md", sourcePath);
                Assert.Equal("TC-1", testCaseId);
                return history;
            });

        var testCase = Assert.Single(Assert.Single(Assert.Single(content.Files).Titles).VerificationCases);
        Assert.Equal(TestResultOutcome.Pass, testCase.PreviousResult?.Outcome);
        Assert.Equal(executedAtUtc.ToLocalTime(), testCase.PreviousResult?.ExecutedAtLocal);
        Assert.Equal(2, testCase.History.Count);
        Assert.Equal("def456", testCase.History[0].SpecificationRevision);
        Assert.Equal("First", testCase.History[1].ExecutedBy);
    }

    [Fact]
    public void Create_represents_missing_history_as_not_tested()
    {
        var file = Parse("verification.md", ValidSpecification("Verification", "Overview"));

        var content = SpecificationPageContent.Create(
            new SpecificationCatalogResult([file], file.Specification.Diagnostics),
            (_, _) => []);

        var testCase = Assert.Single(Assert.Single(Assert.Single(content.Files).Titles).VerificationCases);
        Assert.Null(testCase.PreviousResult);
        Assert.Empty(testCase.History);
    }

    [Fact]
    public void Create_summarizes_only_current_registerable_cases_for_directory_input()
    {
        var source = ValidSpecification("Summary", "Overview").Replace(
            "| TC-1 | Major | Middle | Minor | - | Success |",
            """
            | TC-1 | Major | Middle | One | - | Success |
            | TC-2 | Major | Middle | Two | - | Success |
            | TC-3 | Major | Middle | Three | - | Success |
            | TC-4 | Major | Middle | Four | - | Success |
            | TC-5 | Major | Middle | Five | - | Success |
            """,
            StringComparison.Ordinal);
        var file = Parse("summary.md", source);
        var outcomes = new Dictionary<string, TestResultOutcome>
        {
            ["TC-1"] = TestResultOutcome.Pass,
            ["TC-2"] = TestResultOutcome.Fail,
            ["TC-3"] = TestResultOutcome.Blocked,
            ["TC-4"] = TestResultOutcome.NotApplicable,
        };

        var content = SpecificationPageContent.Create(
            new SpecificationCatalogResult([file], file.Specification.Diagnostics),
            (_, id) => outcomes.TryGetValue(id, out var outcome)
                ? [new TestResultRecord(1, 1, DateTimeOffset.UtcNow, "Tester", id, outcome, null, "abc")]
                : [],
            showFileSummaries: true);

        var summary = Assert.Single(content.Files).Summary;
        Assert.NotNull(summary);
        Assert.Equal(5, summary.Total);
        Assert.Equal(1, summary.Pass);
        Assert.Equal(1, summary.Fail);
        Assert.Equal(1, summary.Blocked);
        Assert.Equal(1, summary.NotApplicable);
        Assert.Equal(1, summary.NotTested);
    }

    [Fact]
    public void Create_omits_file_summary_for_single_file_input()
    {
        var file = Parse("single.md", ValidSpecification("Single", "Overview"));

        var content = SpecificationPageContent.Create(
            new SpecificationCatalogResult([file], file.Specification.Diagnostics),
            showFileSummaries: false);

        Assert.Null(Assert.Single(content.Files).Summary);
    }

    [Fact]
    public void Create_excludes_duplicate_ids_from_the_file_summary()
    {
        var source = ValidSpecification("Duplicates", "Overview").Replace(
            "| TC-1 | Major | Middle | Minor | - | Success |",
            "| TC-1 | Major | Middle | One | - | Success |\n| TC-1 | Major | Middle | Two | - | Success |",
            StringComparison.Ordinal);
        var file = Parse("duplicates.md", source);

        var content = SpecificationPageContent.Create(
            new SpecificationCatalogResult([file], file.Specification.Diagnostics),
            showFileSummaries: true);

        var summary = Assert.Single(content.Files).Summary;
        Assert.NotNull(summary);
        Assert.Equal(0, summary.Total);
        Assert.Equal(0, summary.NotTested);
    }

    private static SpecificationFileLoadResult Parse(string path, string source) =>
        new(path, TestSpecificationParser.Parse(source, path));

    private static string ValidSpecification(string title, string overview) => $$"""
        Testman-Format-Version: 1

        # {{title}}

        ## Overview

        {{overview}}

        ## Preconditions

        None

        ## Common steps

        None

        | ID | Major item | Middle item | Minor item | Steps | Expected result |
        |---|---|---|---|---|---|
        | TC-1 | Major | Middle | Minor | - | Success |
        """;
}
