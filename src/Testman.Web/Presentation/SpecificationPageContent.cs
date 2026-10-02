using Testman.Core.Rendering;
using Testman.Core.Persistence;
using Testman.Core.Specifications;

namespace Testman.Web.Presentation;

public sealed record SpecificationPageContent(
    IReadOnlyList<SpecificationDiagnosticContent> Diagnostics,
    IReadOnlyList<SpecificationFileContent> Files)
{
    public static SpecificationPageContent Create(
        SpecificationCatalogResult catalog,
        Func<string, string, IReadOnlyList<TestResultRecord>>? readHistory = null,
        bool showFileSummaries = false)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var diagnostics = catalog.Diagnostics
            .Select(diagnostic => new SpecificationDiagnosticContent(
                diagnostic.SourcePath,
                diagnostic.LineNumber,
                diagnostic.Reason))
            .ToList();

        var files = catalog.Files
            .Where(file => file.Specification.Titles.Count > 0)
            .Select(file => CreateFile(file, readHistory, showFileSummaries))
            .ToList();

        return new SpecificationPageContent(diagnostics, files);
    }

    private static SpecificationFileContent CreateFile(
        SpecificationFileLoadResult file,
        Func<string, string, IReadOnlyList<TestResultRecord>>? readHistory,
        bool showFileSummary)
    {
        var titles = file.Specification.Titles
            .Select(title => CreateTitle(file.SourcePath, title, readHistory))
            .ToList();
        var currentCases = titles
            .SelectMany(title => title.VerificationCases)
            .Where(testCase => testCase.CanRegisterResult)
            .ToList();

        return new SpecificationFileContent(
            file.SourcePath,
            file.Specification.FormatVersion,
            titles,
            showFileSummary ? CreateSummary(currentCases) : null);
    }

    private static SpecificationFileSummaryContent CreateSummary(
        IReadOnlyList<SpecificationVerificationCaseContent> testCases) =>
        new(
            testCases.Count,
            testCases.Count(item => item.PreviousResult?.Outcome == TestResultOutcome.Pass),
            testCases.Count(item => item.PreviousResult?.Outcome == TestResultOutcome.Fail),
            testCases.Count(item => item.PreviousResult?.Outcome == TestResultOutcome.Blocked),
            testCases.Count(item => item.PreviousResult?.Outcome == TestResultOutcome.NotApplicable),
            testCases.Count(item => item.PreviousResult is null));

    private static SpecificationTitleContent CreateTitle(
        string sourcePath,
        TestSpecificationTitle title,
        Func<string, string, IReadOnlyList<TestResultRecord>>? readHistory) =>
        new(
            title.Name,
            SafeMarkdownRenderer.Render(title.OverviewMarkdown),
            SafeMarkdownRenderer.Render(title.PreconditionsMarkdown),
            SafeMarkdownRenderer.Render(title.CommonStepsMarkdown),
            title.TestCases.Select(testCase => new SpecificationPatternContent(
                testCase.MajorItem,
                testCase.MiddleItem,
                testCase.MinorItem)).ToList(),
            title.TestCases.Select(testCase =>
            {
                var history = readHistory?.Invoke(sourcePath, testCase.Id) ?? [];
                var historyContent = history.Select(CreateResult).ToList();
                return new SpecificationVerificationCaseContent(
                    testCase.Id,
                    SafeMarkdownRenderer.Render(testCase.Steps),
                    SafeMarkdownRenderer.Render(testCase.ExpectedResult),
                    testCase.CanRegisterResult,
                    historyContent.FirstOrDefault(),
                    historyContent);
            }).ToList());

    private static TestResultContent CreateResult(TestResultRecord result) =>
        new(
            result.Outcome,
            result.ExecutedAtUtc.ToLocalTime(),
            result.ExecutedBy,
            result.Comment,
            result.SpecificationRevision);
}

public sealed record SpecificationDiagnosticContent(
    string SourcePath,
    int? LineNumber,
    string Reason);

public sealed record SpecificationFileContent(
    string SourcePath,
    int? FormatVersion,
    IReadOnlyList<SpecificationTitleContent> Titles,
    SpecificationFileSummaryContent? Summary);

public sealed record SpecificationFileSummaryContent(
    int Total,
    int Pass,
    int Fail,
    int Blocked,
    int NotApplicable,
    int NotTested);

public sealed record SpecificationTitleContent(
    string Name,
    string OverviewHtml,
    string PreconditionsHtml,
    string CommonStepsHtml,
    IReadOnlyList<SpecificationPatternContent> Patterns,
    IReadOnlyList<SpecificationVerificationCaseContent> VerificationCases);

public sealed record SpecificationPatternContent(
    string MajorItem,
    string MiddleItem,
    string MinorItem);

public sealed record SpecificationVerificationCaseContent(
    string Id,
    string StepsHtml,
    string ExpectedResultHtml,
    bool CanRegisterResult,
    TestResultContent? PreviousResult,
    IReadOnlyList<TestResultContent> History);

public sealed record TestResultContent(
    TestResultOutcome Outcome,
    DateTimeOffset ExecutedAtLocal,
    string ExecutedBy,
    string? Comment,
    string SpecificationRevision)
{
    public string OutcomeLabel => Outcome switch
    {
        TestResultOutcome.Pass => "Pass",
        TestResultOutcome.Fail => "Fail",
        TestResultOutcome.Blocked => "Blocked",
        TestResultOutcome.NotApplicable => "N/A",
        _ => throw new ArgumentOutOfRangeException(nameof(Outcome)),
    };
}
