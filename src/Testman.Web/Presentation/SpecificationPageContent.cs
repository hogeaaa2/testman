using Testman.Core.Rendering;
using Testman.Core.Persistence;
using Testman.Core.Specifications;

namespace Testman.Web.Presentation;

public sealed record SpecificationPageContent(
    IReadOnlyList<SpecificationDiagnosticContent> Diagnostics,
    IReadOnlyList<SpecificationFileContent> Files,
    IReadOnlyList<SpecificationTestListItemContent> TestList)
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

        var testList = files
            .SelectMany(file => file.Titles.SelectMany(title => title.VerificationCases
                .Where(testCase => testCase.CanRegisterResult)
                .Select(testCase => new SpecificationTestListItemContent(
                    testCase.Id,
                    title.Name,
                    file.SourcePath,
                    testCase.PreviousResult))))
            .ToList();

        return new SpecificationPageContent(diagnostics, files, testList);
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
            CreatePatterns(title.TestCases),
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

    private static IReadOnlyList<SpecificationPatternContent> CreatePatterns(
        IReadOnlyList<TestSpecificationCase> testCases)
    {
        var major = CalculateRowSpans(testCases, item => item.MajorItem, _ => 0);
        var middle = CalculateRowSpans(testCases, item => item.MiddleItem, item => item.MajorItem);
        var minor = CalculateRowSpans(
            testCases,
            item => item.MinorItem,
            item => (item.MajorItem, item.MiddleItem));

        return testCases.Select((testCase, index) => new SpecificationPatternContent(
            testCase.MajorItem,
            testCase.MiddleItem,
            testCase.MinorItem,
            major[index],
            middle[index],
            minor[index])).ToList();
    }

    private static int[] CalculateRowSpans<TParent>(
        IReadOnlyList<TestSpecificationCase> testCases,
        Func<TestSpecificationCase, string> value,
        Func<TestSpecificationCase, TParent> parent)
    {
        var spans = Enumerable.Repeat(1, testCases.Count).ToArray();
        for (var start = 0; start < testCases.Count;)
        {
            var currentValue = value(testCases[start]);
            if (currentValue == "-")
            {
                start++;
                continue;
            }

            var currentParent = parent(testCases[start]);
            var end = start + 1;
            while (end < testCases.Count
                && value(testCases[end]) == currentValue
                && EqualityComparer<TParent>.Default.Equals(parent(testCases[end]), currentParent))
            {
                spans[end] = 0;
                end++;
            }

            spans[start] = end - start;
            start = end;
        }

        return spans;
    }

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

public sealed record SpecificationTestListItemContent(
    string Id,
    string Title,
    string SourcePath,
    TestResultContent? LatestResult);

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
    string MinorItem,
    int MajorItemRowSpan,
    int MiddleItemRowSpan,
    int MinorItemRowSpan);

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
