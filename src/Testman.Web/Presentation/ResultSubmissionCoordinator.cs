using Testman.Core.Persistence;
using Testman.Core.Specifications;

namespace Testman.Web.Presentation;

public sealed record ResultCaseInput(
    string SourcePath,
    string TestCaseId,
    string? Outcome,
    string? Comment);

public sealed record ResultSubmissionRequest(
    string ExecutedBy,
    IReadOnlyList<ResultCaseInput> Cases,
    bool ConfirmPartial);

public enum ResultSubmissionStatus
{
    Saved,
    NeedsPartialConfirmation,
    Invalid,
}

public sealed record ResultSubmissionResponse(ResultSubmissionStatus Status, string Message);

public sealed class ResultSubmissionCoordinator(
    string specificationPath,
    string workingDirectory,
    ResultHistoryStore resultHistory,
    TimeProvider timeProvider)
{
    private static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    public ResultSubmissionResponse Submit(ResultSubmissionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.ExecutedBy))
        {
            return Invalid("Executor name is required.");
        }

        var catalog = SpecificationCatalog.Load(specificationPath, workingDirectory);
        var currentCases = catalog.Files
            .SelectMany(file => file.Specification.Titles
                .SelectMany(title => title.TestCases
                    .Where(testCase => testCase.CanRegisterResult)
                    .Select(testCase => new CurrentCase(
                        Path.GetFullPath(file.SourcePath),
                        testCase.Id))))
            .ToList();

        if (!MatchesCurrentCases(request.Cases, currentCases))
        {
            return Invalid("The submitted test cases no longer match the current specification.");
        }

        var selected = new List<(ResultCaseInput Input, TestResultOutcome Outcome)>();
        foreach (var input in request.Cases)
        {
            if (string.IsNullOrWhiteSpace(input.Outcome))
            {
                continue;
            }

            if (!TryParseOutcome(input.Outcome, out var outcome))
            {
                return Invalid($"Invalid result for {input.TestCaseId}.");
            }

            selected.Add((input, outcome));
        }

        if (selected.Count == 0)
        {
            return Invalid("Select a result for at least one test case.");
        }

        if (selected.Count < currentCases.Count && !request.ConfirmPartial)
        {
            return new ResultSubmissionResponse(
                ResultSubmissionStatus.NeedsPartialConfirmation,
                "Not all test cases have a result. Confirm saving only the selected cases.");
        }

        try
        {
            var references = selected
                .Select(item => Path.GetFullPath(item.Input.SourcePath))
                .Distinct(PathComparer)
                .ToDictionary(path => path, GitSpecificationReference.Resolve, PathComparer);
            var inputs = selected.Select(item => new TestResultInput(
                references[Path.GetFullPath(item.Input.SourcePath)],
                item.Input.TestCaseId,
                item.Outcome,
                item.Input.Comment)).ToList();

            resultHistory.Append(new ResultSubmission(
                timeProvider.GetUtcNow(),
                request.ExecutedBy,
                inputs));
        }
        catch (InvalidOperationException)
        {
            return Invalid("Results can only be saved for committed, unchanged Git specification files.");
        }

        return new ResultSubmissionResponse(ResultSubmissionStatus.Saved, "Results saved.");
    }

    private static bool MatchesCurrentCases(
        IReadOnlyList<ResultCaseInput>? submitted,
        IReadOnlyList<CurrentCase> current)
    {
        if (submitted is null || submitted.Count != current.Count)
        {
            return false;
        }

        var normalized = new List<CurrentCase>();
        try
        {
            foreach (var item in submitted)
            {
                normalized.Add(new CurrentCase(Path.GetFullPath(item.SourcePath), item.TestCaseId));
            }
        }
        catch (Exception exception) when (exception is ArgumentException
            or NotSupportedException
            or PathTooLongException)
        {
            return false;
        }

        return normalized.Distinct(CurrentCaseComparer.Instance).Count() == submitted.Count
            && current.All(expected => normalized.Contains(expected, CurrentCaseComparer.Instance));
    }

    private static bool TryParseOutcome(string value, out TestResultOutcome outcome)
    {
        outcome = value switch
        {
            "pass" => TestResultOutcome.Pass,
            "fail" => TestResultOutcome.Fail,
            "blocked" => TestResultOutcome.Blocked,
            "not_applicable" => TestResultOutcome.NotApplicable,
            _ => (TestResultOutcome)(-1),
        };
        return Enum.IsDefined(outcome);
    }

    private static ResultSubmissionResponse Invalid(string message) =>
        new(ResultSubmissionStatus.Invalid, message);

    private sealed record CurrentCase(string SourcePath, string TestCaseId);

    private sealed class CurrentCaseComparer : IEqualityComparer<CurrentCase>
    {
        public static CurrentCaseComparer Instance { get; } = new();

        public bool Equals(CurrentCase? left, CurrentCase? right) =>
            ReferenceEquals(left, right)
            || left is not null
            && right is not null
            && PathComparer.Equals(left.SourcePath, right.SourcePath)
            && StringComparer.Ordinal.Equals(left.TestCaseId, right.TestCaseId);

        public int GetHashCode(CurrentCase value) => HashCode.Combine(
            PathComparer.GetHashCode(value.SourcePath),
            StringComparer.Ordinal.GetHashCode(value.TestCaseId));
    }
}
